using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using LiveDc.Managers;
using LiveDc.Notify;
using LiveDc.Properties;
using LiveDc.Providers;
using Microsoft.Win32;
using MonoTorrent.Common;
using SharpDc;
using SharpDc.Structs;
using Win32;
using DataReceivedEventArgs = Win32.DataReceivedEventArgs;
using LogManager = SharpDc.Logging.LogManager;
using Timer = System.Windows.Forms.Timer;

namespace LiveDc
{
    /// <summary>
    /// General entity that incorporates different p2p protocols and provides common stuff like settings, history, etc
    /// </summary>
    public partial class LiveClient
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private DateTime _hideTime;
        private NotifyIcon _icon;
        private Timer _timer;
        
        private readonly AsyncOperation _ao;
        private LiveDcDrive _drive;
        private readonly CopyData _copyData;
        private readonly List<Tuple<Action, string>> _importantActions = new List<Tuple<Action, string>>();
        private readonly List<IP2PProvider> _providers = new List<IP2PProvider>();
        private readonly List<IFsProvider> _fsProviders = new List<IFsProvider>();
        private IStartItem _startItem;
        private FrmStatus _statusForm;
        private readonly HttpProvider _httpProvider;
        private readonly DcProvider _dcProvider;
        private StorageManager _storageManager;

        private string DriveLockPath { get { return Path.Combine(Settings.SettingsFolder, "drive.lck"); } }
        
        public Settings Settings { get; private set; }
        public LiveHistoryManager History { get; private set; }
        public AutoUpdateManager AutoUpdate { get; private set; }
        public AsyncOperation AsyncOperation { get { return _ao; } }
        public LiveDcDrive Drive { get { return _drive; } }
        public IEnumerable<IP2PProvider> Providers { get { return _providers; } }
        public IEnumerable<IFsProvider> FsProviders { get { return _providers.Concat(_fsProviders); } }

        public HttpProvider HttpProvider
        {
            get { return _httpProvider; }
        }
        
        public LiveClient()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            Application.ApplicationExit += ApplicationApplicationExit;
            
            InitializeComponent();

            Settings = new Settings();
            if (File.Exists(Settings.SettingsFilePath))
                Settings.Load();

            LogManager.LogManagerInstance = new NLogManager();
            MonoTorrent.LogManager.LogManagerInstance = new TorrentNLogManager();

            logger.Info("------------------------------starting LiveDC {0}", Assembly.GetExecutingAssembly().GetName().Version);
            
            _copyData = new CopyData();
            _copyData.CreateHandle(new CreateParams());
            _copyData.Channels.Add("LIVEDC");
            _copyData.DataReceived += CopyDataDataReceived;

            _ao = AsyncOperationManager.CreateOperation(null);


            Utils.FileSizeFormatProvider.BinaryModifiers = new[] { " Б", " КБ", " МБ", " ГБ", " ТБ", " ПБ" };



            _providers.Add(_dcProvider = new DcProvider(Settings, this));
            _providers.Add(new TorrentProvider(this));

            _fsProviders.Add(_httpProvider = new HttpProvider(this));

            InitializeEngine();

            History = new LiveHistoryManager();
            History.Load();

            AutoUpdate = new AutoUpdateManager(this);
            AutoUpdate.CheckUpdate();
            
            if (!Settings.ShownGreetingsTooltip)
            {
                _icon.ShowBalloonTip(10000, "LiveDC", "Добро пожаловать! Нажмите здесь, чтобы увидеть текущий статус работы.", ToolTipIcon.Info);
                OpenStartPage();

                Settings.ShownGreetingsTooltip = true;
                Settings.Save();
            }
            else if (!Program.SilentMode)
            {
                OpenStartPage();
                _icon.ShowBalloonTip(10000, "LiveDC", "Клиент запущен.", ToolTipIcon.Info);
            }

            if (!string.IsNullOrEmpty(Program.StartMagnet))
            {
                StartFile(Magnet.Parse(Program.StartMagnet));
            }

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        /// <summary>
        /// Checks all possible cache locations for specified amount of bytes and returns first
        /// If none has enough space removes old cached files 
        /// </summary>
        /// <param name="bytesToReserve"></param>
        /// <returns>Path to root cache directory or null if there is no free space</returns>
        public string ReserveCacheSpace(long bytesToReserve)
        {
            string root;

            var freed = 0L;

            while (( root = _storageManager.GetStoragePoint(bytesToReserve) ) == null)
            {
                if (!Settings.StorageAutoPrune)
                    return null;

                var round = 0L;

                foreach (var p2PProvider in Providers)
                {
                    round += p2PProvider.ReleaseFileCache(bytesToReserve);

                    if (round > 0 && freed + round >= bytesToReserve)
                        break;
                }

                if (round == 0)
                    return null;

                freed += round;
            }

            return root;
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                _dcProvider.Engine.Connect();
            }
        }
        
        private void _icon_MouseClick(object sender, MouseEventArgs e)
        {
            if (_importantActions.Count > 0)
            {
                var action = _importantActions[0];
                action.Item1();
                _importantActions.RemoveAt(0);
                UpdateTrayIcon();
                return;
            }
            
            if ((DateTime.Now - _hideTime).TotalMilliseconds < 300)
                return;

            if (_form == null)
            {
                _form = new FrmNotify(this);
                _form.Deactivate += form_Deactivate;
                _form.UpdateWindowPos(_icon);
                _form.Show();
                _form.Hide();
            }
            else
            {
                _form.UpdateWindowPos(_icon);
            }

            _form.Show();
            _form.Activate();
            _form.TopMost = true;
        }

        private void form_Deactivate(object sender, EventArgs e)
        {
            if (_form.Visible)
                _hideTime = DateTime.Now;

            var form = (Form)sender;
            form.Hide();
        }

        private void ApplicationApplicationExit(object sender, EventArgs e)
        {
            logger.Info("Preparing to exit...");
            Dispose();
            logger.Info("Ready to exit");
        }
        
        private void InitializeEngine()
        {
            if (File.Exists(DriveLockPath))
            {
                var text = File.ReadAllText(DriveLockPath);

                if (text.Length > 0)
                {
                    var letter = text[0];

                    if (char.IsLetter(letter))
                    {
                        LiveDcDrive.Unmount(letter);
                    }
                }
                File.Delete(DriveLockPath);
            }

            _providers.ForEach(p => p.Initialize());

            #region Virtual drive
            char driveLetter;

            if (!string.IsNullOrEmpty(Settings.VirtualDriveLetter))
            {
                if (!StorageHelper.IsDriveFree(Settings.VirtualDriveLetter[0]))
                    driveLetter = StorageHelper.GetFreeDrive('l');
                else
                {
                    driveLetter = Settings.VirtualDriveLetter[0];
                }
            }
            else
            {
                driveLetter = StorageHelper.GetFreeDrive('l');
            }

            HttpFileStream.Manager.CacheSize = 30 * 1024 * 1024;
            
            _drive = new LiveDcDrive(FsProviders);
            _drive.MountAsync(driveLetter);
            StorageHelper.OwnDrive = char.ToUpper(driveLetter) + ":\\";
            _storageManager = new StorageManager(StorageHelper.OwnDrive);

            if (!Directory.Exists(Path.GetDirectoryName(DriveLockPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(DriveLockPath));

            File.WriteAllText(DriveLockPath, driveLetter.ToString());
            #endregion


        }

        public void AddClickAction(Action action, string balloonText, string actionId = null)
        {
            AsyncOperation.Post(o => {
                if (!string.IsNullOrEmpty(balloonText) && !Program.SilentMode)
                    _icon.ShowBalloonTip(8000, "LiveDC", balloonText, ToolTipIcon.None);

                _importantActions.Add(Tuple.Create(action, actionId));

                UpdateTrayIcon();
            }, null);
        }

        public void RemoveActionById(string id)
        {
            if (id == null) 
                throw new ArgumentNullException("id");

            _importantActions.RemoveAll(t => t.Item2 == id);

            if (_importantActions.Count == 0)
            {
                AsyncOperation.Post(o => UpdateTrayIcon(), null);
            }
        }

        private bool OpenStartPage()
        {
            if (Settings.OpenStartPage && !string.IsNullOrEmpty(Settings.StartPageUrl))
            {
                ShellHelper.Start(Settings.StartPageUrl);
                return true;
            }

            return false;
        }

        private void CopyDataDataReceived(object sender, DataReceivedEventArgs e)
        {
            var data = e.Data.ToString();

            if (data == "SHOW")
            {
                if (!OpenStartPage())
                    _icon.ShowBalloonTip(5000, "Я здесь!", "Клиент LiveDC уже запущен", ToolTipIcon.None);
                return;
            }

            if (data.StartsWith("magnet:"))
            {
                var magnet = Magnet.Parse(data);
                StartFile(magnet);
                return;
            }

            if (data.EndsWith(".torrent"))
            {
                StartFile(data);
            }
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                logger.Fatal("Unhandled exception: {0} at {1}", exception.Message, exception.StackTrace);
            }

            if (_drive != null)
            {
                // it is critically important to release the virtual drive
                _drive.Unmount();
                File.Delete(DriveLockPath);
            }
        }

        private void ProgramExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void EngineActivated(object sender, EventArgs e)
        {
            UpdateTrayIcon();
            UpdateTrayText();
        }

        public void UpdateTrayIcon()
        {
            if (_importantActions.Count > 0)
            {
                _icon.Icon = Resources.livedc_action;
                return;
            }

            if (_providers.Any(p => p.Online))
            {
                _icon.Icon = Resources.livedc;
            }
            else
            {
                _icon.Icon = Resources.livedc_offline;
            }
        }

        private void UpdateTrayText()
        {
            var text = _providers.Any(p => p.Online) ? "Статус: в сети" : "Статус: отключен";
            Fixes.SetNotifyIconText(_icon, text);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            UpdateTrayText();
        }

        public void StartFile(string torrentFilePath)
        {
            if (!Drive.IsReady)
            {
                MessageBox.Show("Не удалось подключить виртуальный диск. Попробуйте перезагрузить компьютер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var torrent = Torrent.Load(torrentFilePath);
                var torrentProvider = Providers.OfType<TorrentProvider>().First();
                
                _startItem = torrentProvider.StartItem(torrent);

                if (_statusForm == null)
                    _statusForm = new FrmStatus(_ao);

                if (!_startItem.ReadyToStart)
                    _statusForm.UpdateAndShow(_startItem);
            }
            catch (Exception x)
            {
                MessageBox.Show("Произошла ошибка при попытке запуска торрент файла: " + x.Message);
            }
        }

        public void StartFile(Magnet magnet)
        {
            if (!Drive.IsReady)
            {
                MessageBox.Show("Не удалось подключить виртуальный диск. Попробуйте перезагрузить компьютер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var provider = FsProviders.First(p => p.CanHandle(magnet));

                _startItem = provider.StartItem(magnet);

                if (_statusForm == null)
                    _statusForm = new FrmStatus(_ao);

                if (!_startItem.ReadyToStart)
                    _statusForm.UpdateAndShow(_startItem);
            }
            catch (Exception x)
            {
                MessageBox.Show("Не удается начать загрузку: " + x.Message);
            }
        }
        
        public void Dispose()
        {
            if (_drive != null)
            {
                _drive.Unmount();
                File.Delete(DriveLockPath);
            }

            History.Save();
            
            _providers.ForEach(p => p.Dispose());

            if (_icon != null)
                _icon.Visible = false;
        }

        public void UpdateFileItem(DcFileControl control)
        {
            var provider = _providers.First(p => p.CanHandle(control.Magnet));

            provider.UpdateFileItem(control);
        }

        public void DeleteItem(Magnet magnet)
        {
            var provider = _providers.First(p => p.CanHandle(magnet));

            History.DeleteItem(magnet);
            provider.DeleteFile(magnet);
        }
    }

    public class Fixes
    {
        public static void SetNotifyIconText(NotifyIcon ni, string text)
        {
            if (text.Length >= 128) 
                throw new ArgumentOutOfRangeException("Text limited to 127 characters");
            Type t = typeof(NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);
            if ((bool)t.GetField("added", hidden).GetValue(ni))
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
        }
    }
}

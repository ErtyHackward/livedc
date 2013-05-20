using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using LiveDc.Managers;
using LiveDc.Notify;
using LiveDc.Properties;
using SharpDc;
using SharpDc.Interfaces;
using SharpDc.Logging;
using SharpDc.Managers;
using SharpDc.Structs;
using Win32;
using DataReceivedEventArgs = Win32.DataReceivedEventArgs;
using Timer = System.Windows.Forms.Timer;

namespace LiveDc
{
    public partial class LiveClient
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private DateTime _hideTime;
        private NotifyIcon _icon;
        private Timer _timer;

        private DcEngine _engine;
        private AsyncOperation _ao;
        private LiveDcDrive _drive;
        private CopyData _copyData;
        
        private List<Tuple<Action, string>> _importantActions = new List<Tuple<Action, string>>();

        private HubManager _hubManager;

        public HubManager HubManager { get { return _hubManager; } }

        public Settings Settings { get; private set; }

        public DcEngine Engine
        {
            get { return _engine; }
        }

        public LiveHistoryManager History { get; private set; }

        public AutoUpdateManager AutoUpdate { get; private set; }
        
        public AsyncOperation AsyncOperation
        {
            get { return _ao; }
        }

        public LiveDcDrive Drive { get { return _drive; } }

        public LaunchManager LaunchManager { get; set; }

        private string IncompletePath { get { return Path.Combine(Settings.SettingsFolder, "downloads.xml"); } }

        private string SharePath { get { return Path.Combine(Settings.SettingsFolder, "share.xml"); } }

        private string DriveLockPath { get { return Path.Combine(Settings.SettingsFolder, "drive.lck"); } }

        public LiveClient()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            Application.ApplicationExit += ApplicationApplicationExit;
            

            if (!WindowsHelper.IsMagnetHandlerAssigned)
            {
                var res = WindowsHelper.RegisterMagnetHandler();
                //if (MessageBox.Show("Хотите чтобы LiveDC обрабатывал магнет-ссылки ?", "Вопрос", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                //{
                //    VistaSecurity.StartElevated("-reg");
                //}
            }

            InitializeComponent();

            Settings = new Settings();
            if (File.Exists(Settings.SettingsFilePath))
                Settings.Load();

            LogManager.LogManagerInstance = new NLogManager();

            _copyData = new CopyData();
            _copyData.CreateHandle(new CreateParams());
            _copyData.Channels.Add("LIVEDC");
            _copyData.DataReceived += CopyDataDataReceived;

            _ao = AsyncOperationManager.CreateOperation(null);


            Utils.FileSizeFormatProvider.BinaryModifiers = new[] { " Б", " КБ", " МБ", " ГБ", " ТБ", " ПБ" };

            InitializeEngine();

            History = new LiveHistoryManager(this);
            History.Load();

            AutoUpdate = new AutoUpdateManager(this);
            AutoUpdate.CheckUpdate();

            LaunchManager = new LaunchManager(this);

            LiveApi.CheckPortAsync(_engine.Settings.TcpPort, PortCheckComplete);

            if (!Settings.ShownGreetingsTooltip)
            {
                _icon.ShowBalloonTip(10000, "LiveDC", "Добро пожаловать! Нажмите на этот значок чтобы увидеть текущий статус работы.", ToolTipIcon.Info);

                Settings.ShownGreetingsTooltip = true;
                Settings.Save();
            }
            else if (!Program.SilentMode)
            {
                _icon.ShowBalloonTip(10000, "LiveDC", "Клиент запущен.", ToolTipIcon.Info);
            }

            if (!string.IsNullOrEmpty(Program.StartMagnet))
            {
                LaunchManager.StartFile(Magnet.Parse(Program.StartMagnet));
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
        }

        private void form_Deactivate(object sender, EventArgs e)
        {
            var form = (Form)sender;
            form.Hide();
            _hideTime = DateTime.Now;
        }

        void ApplicationApplicationExit(object sender, EventArgs e)
        {
            logger.Info("Preparing to exit...");
            Dispose();
            logger.Info("Ready to exit");
        }

        private void PortCheckComplete(CheckIpResult e)
        {
            if (e.IsPortOpen)
            {
                _engine.Settings.LocalAddress = e.ExternalIpAddress;
                Settings.IPAddress = e.ExternalIpAddress;
            }

            _engine.Settings.ActiveMode = e.IsPortOpen;

            if (string.IsNullOrEmpty(Settings.Hubs))
            {
                _hubManager.FindHubs(IPAddress.Parse(e.ExternalIpAddress));
            }
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
            
            _engine = new DcEngine();
            _engine.Settings.ActiveMode = Settings.ActiveMode;
            _engine.Settings.UseSparseFiles = true;
            _engine.Settings.AutoSelectPort = true;
            _engine.TagInfo.Version = "livedc";

            _hubManager = new HubManager(_engine, this);

            if (File.Exists(SharePath))
            {
                try
                {
                    _engine.Share = MemoryShare.CreateFromXml(SharePath);
                }
                catch (Exception x)
                {
                    logger.Error("Unable to load share from {0} because {1}", SharePath, x.Message);
                }
            }

            if (_engine.Share == null)
            {
                _engine.Share = new MemoryShare();
            }

            if (Settings.StorageAutoSelect)
            {
                _engine.Settings.PathDownload = StorageHelper.GetBestSaveDirectory();
            }
            else
            {
                _engine.Settings.PathDownload = Settings.StoragePath;
            }

            if (File.Exists(IncompletePath))
            {
                try
                {
                    _engine.DownloadManager.Load(IncompletePath);
                }
                catch (Exception x)
                {
                    logger.Error("Unable to load downloads {0}", x.Message);
                }   
            }

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

            _drive = new LiveDcDrive(_engine);
            _drive.MountAsync(driveLetter);

            if (!Directory.Exists(Path.GetDirectoryName(DriveLockPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(DriveLockPath));

            File.WriteAllText(DriveLockPath, driveLetter.ToString());
            #endregion

            Settings.Nickname = "livedc" + Guid.NewGuid().ToString().GetMd5Hash().Substring(0, 8);

            _engine.ActiveStatusChanged += EngineActivated;

            _engine.StartAsync();
            _engine.Connect();
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

        void CopyDataDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.ToString() == "SHOW")
            {
                _icon.ShowBalloonTip(5000, "Я здесь!", "Клиент LiveDC уже запущен", ToolTipIcon.None);
                return;
            }

            var magnet = Magnet.Parse((string)e.Data);
            LaunchManager.StartFile(magnet);
        }
        
        void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (_drive != null)
            {
                // it is critically important to release the virtual drive
                _drive.Unmount();
                File.Delete(DriveLockPath);
            }
        }

        void ProgramExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        void EngineActivated(object sender, EventArgs e)
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

            if (_engine.Active)
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
            var text = _engine.Active ? "Статус: в сети" : "Статус: отключен";

            //if (_currentDownload != null)
            //{
            //    var fileName = _currentDownload.Magnet.FileName;

            //    if (fileName.Length > 30)
            //    {
            //        var name = Path.GetFileNameWithoutExtension(fileName);
            //        var ext = Path.GetExtension(fileName);

            //        fileName = name.Substring(0, 30 - ext.Length) + "..." + ext;
            //    }

            //    text = string.Format("{0}\n{1}/{2} ({3}/с)\n{4} ",
            //        fileName,
            //        Utils.FormatBytes(_engine.DownloadManager.GetTotalDownloadBytes(_currentDownload)),
            //        Utils.FormatBytes(_currentDownload.Magnet.Size),
            //        Utils.FormatBytes(_engine.TransferManager.GetDownloadSpeed(t => t.DownloadItem == _currentDownload)),
            //        text);
            //}
            Fixes.SetNotifyIconText(_icon, text);
        }

        void TimerTick(object sender, EventArgs e)
        {
            UpdateTrayText();
        }
        
        internal void Dispose()
        {
            if (_drive != null)
            {
                _drive.Unmount();
                File.Delete(DriveLockPath);
            }

            History.Save();
            
            if (_engine != null)
            {
                _engine.DownloadManager.Save(IncompletePath);
                
                var share = (MemoryShare)_engine.Share;

                try
                {
                    if (share.IsDirty)
                        share.ExportAsXml(SharePath);
                }
                catch (Exception x)
                {
                    logger.Error("Share save error: {0}", x.Message);
                }
                _engine.Dispose();
            }

            if (_icon != null)
                _icon.Visible = false;
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

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using LiveDc.Properties;
using LiveDc.Utilites;
using SharpDc;
using SharpDc.Connections;
using SharpDc.Events;
using SharpDc.Interfaces;
using SharpDc.Logging;
using SharpDc.Managers;
using SharpDc.Messages;
using SharpDc.Structs;
using Win32;
using DataReceivedEventArgs = Win32.DataReceivedEventArgs;
using Timer = System.Windows.Forms.Timer;

namespace LiveDc
{
    public partial class LiveClient
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        private NotifyIcon _icon;
        private FrmStatus _statusForm;
        private Timer _timer;

        private DcEngine _engine;
        private AsyncOperation _ao;
        private LiveDcDrive _drive;
        private CopyData _copyData;
        private DownloadItem _currentDownload;

        private HubManager _hubManager;

        public Settings Settings { get; private set; }

        public DcEngine Engine
        {
            get { return _engine; }
        }

        public LiveHistoryManager History { get; private set; }

        public AsyncOperation AsyncOperation
        {
            get { return _ao; }
        }

        public LiveDcDrive Drive { get { return _drive; } }

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
            _statusForm = new FrmStatus();


            Utils.FileSizeFormatProvider.BinaryModifiers = new[] { " Б", " КБ", " МБ", " ГБ", " ТБ", " ПБ" };

            InitializeEngine();

            History = new LiveHistoryManager(this);
            History.Load();

            LiveCheckIp.CheckPortAsync(_engine.Settings.TcpPort, PortCheckComplete);

            if (!Settings.ShownGreetingsTooltip)
            {
                _icon.ShowBalloonTip(10000, "LiveDC", "Добро пожаловать! Наведите курсор на этот значок чтобы увидеть текущий статус работы.", ToolTipIcon.Info);

                Settings.ShownGreetingsTooltip = true;
                Settings.Save();
            }

            if (!string.IsNullOrEmpty(Program.StartMagnet))
            {
                StartFile(Magnet.Parse(Program.StartMagnet));
            }
        }

        void ApplicationApplicationExit(object sender, EventArgs e)
        {
            Dispose();
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
        
        void SettingsClick(object sender, EventArgs e)
        {

        }

        void CopyDataDataReceived(object sender, DataReceivedEventArgs e)
        {
            var magnet = Magnet.Parse((string)e.Data);
            StartFile(magnet);
        }

        public void StartFile(Magnet magnet)
        {
            try
            {
                var existingItem = _engine.Share.SearchByTth(magnet.TTH);

                if (existingItem != null)
                {
                    Process.Start(existingItem.Value.SystemPath);
                    return;
                }

                if (_hubManager.InitializationCompleted && !_engine.Active)
                {
                    MessageBox.Show("Не удалось установить соединение ни с одним из хабов. Проверьте сетевое подключение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_drive.IsReady)
                {
                    MessageBox.Show("Не удалось подключить виртуальный диск. Попробуйте перезагрузить компьютер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (_currentDownload != null && _currentDownload.Magnet.TTH == magnet.TTH)
                {
                    Process.Start(Path.Combine(_drive.DriveRoot, _currentDownload.Magnet.FileName));
                    return;
                }

                if (_currentDownload != null)
                    _engine.RemoveDownload(_currentDownload);

                History.AddItem(magnet);

                _currentDownload = _engine.DownloadFile(magnet);
                _currentDownload.LogSegmentEvents = true;

                new ThreadStart(FormThread).BeginInvoke(null, null);
            }
            catch (Exception x)
            {
                MessageBox.Show("Не удается начать загрузку: " + x.Message);
            }
        }

        private void FormThread()
        {
            _ao.Post((o) => { _statusForm.Show(); }, null);

            var sw = Stopwatch.StartNew();

            while (!_hubManager.InitializationCompleted)
            {
                UpdateMessage("Подключение к хабам","");
                Thread.Sleep(100);
            }

            while (sw.Elapsed.Seconds < 20)
            {
                if (_currentDownload.DoneSegmentsCount == 0)
                {
                    UpdateMessage(string.Format("{0} ({1})",_currentDownload.Magnet.FileName,Utils.FormatBytes(_currentDownload.Magnet.Size)), string.Format("Поиск. Найдено {0} источников ",_currentDownload.Sources.Count));
                }
                else
                {
                    UpdateMessage(string.Format("{0} ({1})", _currentDownload.Magnet.FileName, Utils.FormatBytes(_currentDownload.Magnet.Size)), " Открываю файл... ");
                    Process.Start(Path.Combine(_drive.DriveRoot, _currentDownload.Magnet.FileName));
                    break;
                }

                if (_statusForm.DialogResult == DialogResult.Cancel)
                    break;

                Thread.Sleep(100);
            }

            if (_currentDownload.DoneSegmentsCount == 0)
            {
                UpdateMessage(string.Format("{0} ({1})", _currentDownload.Magnet.FileName, Utils.FormatBytes(_currentDownload.Magnet.Size)), string.Format("Не удается начать загрузку. Источников {0}.", _currentDownload.Sources.Count));
            }

            Thread.Sleep(3000);

            if (!_statusForm.Visible)
            {
                _engine.RemoveDownload(_currentDownload);
                _currentDownload = null;
            }
            
            _ao.Post((o) => _statusForm.Hide(), null);
        }

        private void UpdateMessage(string message, string status)
        {
            _ao.Post((o) =>
                         {
                             _statusForm.MainText = message;
                             _statusForm.StatusText = status;
                         }, null);
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
            if (_engine.Active)
            {
                _icon.Icon = Resources.livedc;
            }
            else
            {
                _icon.Icon = Resources.livedc_offline;
            }

            UpdateTrayText();
        }

        private void UpdateTrayText()
        {
            var text = _engine.Active ? "Статус: в сети" : "Статус: отключен";

            if (_currentDownload != null)
            {
                var fileName = _currentDownload.Magnet.FileName;

                if (fileName.Length > 30)
                {
                    var name = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);

                    fileName = name.Substring(0, 30 - ext.Length) + "..." + ext;
                }

                text = string.Format("{0}\n{1}/{2} ({3}/с)\n{4} ",
                    fileName,
                    Utils.FormatBytes(_engine.DownloadManager.GetTotalDownloadBytes(_currentDownload)),
                    Utils.FormatBytes(_currentDownload.Magnet.Size),
                    Utils.FormatBytes(_engine.TransferManager.GetDownloadSpeed(t => t.DownloadItem == _currentDownload)),
                    text);
            }
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

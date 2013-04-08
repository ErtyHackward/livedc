﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using LiveDc.Properties;
using LiveDc.Utilites;
using Microsoft.Win32;
using SharpDc;
using SharpDc.Logging;
using SharpDc.Managers;
using SharpDc.Structs;
using Win32;
using DataReceivedEventArgs = Win32.DataReceivedEventArgs;

namespace LiveDc
{
    public partial class LiveClient
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private DcEngine _engine;
        private NotifyIcon _icon;
        private LiveDcDrive _drive;
        private CopyData _copyData;
        private DownloadItem _currentDownload;
        private FrmStatus _statusForm;
        private AsyncOperation _ao;

        public Settings Settings { get; private set; }

        public DcEngine Engine
        {
            get { return _engine; }
        }

        public AsyncOperation AsyncOperation
        {
            get { return _ao; }
        }

        public LiveClient()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            Application.ApplicationExit += ApplicationApplicationExit;

            if (!WindowsHelper.IsMagnetHandlerAssigned)
            {
                if (MessageBox.Show("Хотите чтобы LiveDC обрабатывал магнет-ссылки ?", "Вопрос", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    VistaSecurity.StartElevated("-reg");
                    return;
                }
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
            
            InitializeEngine();

            LiveCheckIp.CheckPortAsync(_engine.Settings.TcpPort, PortCheckComplete);
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
                if (FlyLinkHelper.IsInstalled)
                {
                    var hubs = FlyLinkHelper.ReadHubs();

                    for (int i = 0; i < hubs.Count; i++)
                    {
                        if (hubs[i].StartsWith("dchub://"))
                            hubs[i] = hubs[i].Remove(0, 8);
                    }

                    Settings.Hubs = string.Join(";", hubs);
                    Settings.Save();
                    foreach (var hub in hubs)
                    {
                        AddHub(hub);
                    }
                }

                //if (StrongDcHelper.IsInstalled)
                //{
                //    var hubs = StrongDcHelper.ReadHubs();

                //    for (int i = 0; i < hubs.Count; i++)
                //    {
                //        if (hubs[i].StartsWith("dchub://"))
                //            hubs[i] = hubs[i].Remove(0, 8);
                //    }

                //    Settings.Hubs = string.Join(";", hubs);
                //    Settings.Save();
                //    foreach (var hub in hubs)
                //    {
                //        AddHub(hub);
                //    }
                //}

                IpGeoBase.RequestAsync(IPAddress.Parse(e.ExternalIpAddress), CityReceived);
            }
        }

        private void CityReceived(IpGeoBaseResponse e)
        {
            if (e.City != null)
            {
                LiveHubs.GetHubsAsync(e.City, HubsListReceived);
                if (!string.IsNullOrEmpty(Settings.Hubs))
                {
                    LiveHubs.PostHubsAsync(e.City, Settings.Hubs);
                }
            }

            _ao.Post((o) => new FrmHubList(this).Show(), null);
        }

        private void HubsListReceived(List<string> list)
        {
            if (list.Count > 0)
            {
                if (Settings.Hubs == null)
                    Settings.Hubs = "";
                else
                    Settings.Hubs += ";";

                Settings.Hubs += string.Join(";", list);
                Settings.Save();
            }
            list.ForEach(AddHub);
        }

        private void AddHub(string hubAddress)
        {
            if (_engine.Hubs.All().Any(h => h.Settings.HubAddress == hubAddress))
                return;

            var hub = _engine.Hubs.Add(hubAddress, Settings.Nickname);
            hub.Settings.GetUsersList = false;
        }

        private string SharePath { get { return Path.Combine(Settings.SettingsFolder, "share.xml"); } }

        private void InitializeEngine()
        {
            _engine = new DcEngine();
            _engine.Settings.ActiveMode = Settings.ActiveMode;
            _engine.TagInfo.Version = "livedc";


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
            #endregion

            Settings.Nickname = "livedc" + Guid.NewGuid().ToString().GetMd5Hash().Substring(0, 8);

            if (!string.IsNullOrEmpty(Settings.Hubs))
            {
                var hubs = Settings.Hubs.Split(';');

                foreach (var hubAddress in hubs)
                {
                    AddHub(hubAddress);
                }
            }

            _engine.ActiveStatusChanged += EngineActivated;

            _engine.StartAsync();
            _engine.Connect();
        }

        void SettingsClick(object sender, EventArgs e)
        {

        }

        void CopyDataDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!_engine.Active)
                {
                    MessageBox.Show("Не удалось установить соединение ни с одним из хабов. Проверьте сетевое подключение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_drive.IsReady)
                {
                    MessageBox.Show("Не удалось подключить виртуальный диск. Попробуйте перезагрузить компьютер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; 
                }

                if (_currentDownload != null)
                    _engine.RemoveDownload(_currentDownload);

                var magnet = Magnet.Parse((string)e.Data);
                _currentDownload = _engine.DownloadFile(magnet);

                new ThreadStart(FormThread).BeginInvoke(null, null);
            }
            catch (Exception x)
            {
                MessageBox.Show("Не удается начать загрузку: " + x.Message);
            }
        }

        private void FormThread()
        {
            _ao.Post((o) => _statusForm.Show(), null);

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.Seconds < 10)
            {
                if (_currentDownload.DoneSegmentsCount == 0)
                {
                    UpdateMessage(_currentDownload.Magnet.FileName, string.Format("{0}. Поиск. Найдено {1} источников ",
                                                Utils.FormatBytes(_currentDownload.Magnet.Size), _currentDownload.Sources.Count));
                }
                else
                {
                    UpdateMessage(_currentDownload.Magnet.FileName, string.Format("{0}. Открываю файл... ",
                                                Utils.FormatBytes(_currentDownload.Magnet.Size)));
                    Process.Start(Path.Combine(_drive.DriveRoot, _currentDownload.Magnet.FileName));
                    break;
                }

                if (_statusForm.DialogResult == DialogResult.Cancel)
                    break;

                Thread.Sleep(100);
            }

            if (_currentDownload.DoneSegmentsCount == 0)
            {
                UpdateMessage(_currentDownload.Magnet.FileName, string.Format("{0}. Не удается начать загрузку. Источников {1}.",
                            Utils.FormatBytes(_currentDownload.Magnet.Size), _currentDownload.Sources.Count));
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
                _icon.Text = "Статус: в сети";
                _icon.Icon = Resources.livedc;
            }
            else
            {
                _icon.Icon = Resources.livedc_offline;
                _icon.Text = "Статус: отключен";
            }
        }
        
        internal void Dispose()
        {
            var share = (MemoryShare)_engine.Share;

            try
            {
                if (share.IsDirty)
                    share.ExportAsXml(SharePath);
            }
            catch (Exception x)
            {
                logger.Error("Share save error: {0}", x.Message );
            }
            
            _drive.Unmount();
            _engine.Dispose();
            _icon.Visible = false;
        }
    }
}

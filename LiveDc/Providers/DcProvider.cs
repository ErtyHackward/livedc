using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using LiveDc.Helpers;
using LiveDc.Managers;
using LiveDc.Notify;
using SharpDc;
using SharpDc.Interfaces;
using SharpDc.Managers;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public class DcProvider : IP2PProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        private string IncompletePath { get { return Path.Combine(Settings.SettingsFolder, "downloads.xml"); } }
        private string SharePath { get { return Path.Combine(Settings.SettingsFolder, "share.xml"); } }
        
        private DcHubManager _hubManager;
        private DcEngine _engine;
        private readonly LiveClient _client;

        public Settings Settings { get; set; }
        public DcEngine Engine { get { return _engine; } }
        public DcHubManager HubManager { get { return _hubManager; } }
        public bool Online { get { return _engine.Active; } }
        public LiveClient LiveClient { get { return _client; } }

        public event EventHandler StatusChanged;

        protected virtual void OnStatusChanged()
        {
            var handler = StatusChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
        
        public DcProvider(Settings settings, LiveClient client)
        {
            _client = client;
            Settings = settings;
            if (!WindowsHelper.IsMagnetHandlerAssigned)
            {
                WindowsHelper.RegisterMagnetHandler();
            }

            LiveApi.CheckPortAsync(settings.TCPPort, PortCheckComplete);
        }
        
        /// <summary>
        /// Groups shared files and currently downloading ones
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Magnet> AllMagnets()
        {
            if (_engine.Share != null)
            {
                foreach (var item in _engine.Share.Items())
                {
                    yield return item.Magnet;
                }
            }

            foreach (var downloadItem in _engine.DownloadManager.Items())
            {
                yield return downloadItem.Magnet;
            }
        }

        public Stream GetStream(Magnet magnet)
        {
            return _engine.GetStream(magnet);
        }

        public bool CanHandle(Magnet magnet)
        {
            return !string.IsNullOrEmpty(magnet.TTH) && magnet.TTH.Length == 39;
        }

        public IStartItem StartItem(Magnet magnet)
        {
            return new DcStartItem(magnet, this);
        }

        public string GetVirtualPath(Magnet magnet)
        {
            return Path.Combine(_client.Drive.DriveRoot, magnet.FileName);
        }

        public string GetRealPath(Magnet magnet)
        {
            throw new NotImplementedException();
        }

        public void UpdateFileItem(DcFileControl control)
        {
            var di = Engine.DownloadManager.GetDownloadItem(control.Magnet.TTH);

            if (di != null)
            {
                control.DownloadedBytes = Engine.DownloadManager.GetTotalDownloadBytes(di);
                control.DownloadSpeed = (long)Engine.TransferManager.GetDownloadSpeed(t => t.DownloadItem == di);
            }
            else
            {
                control.DownloadSpeed = 0;
            }
        }

        public void DeleteFile(Magnet magnet)
        {
            var di = Engine.DownloadManager.GetDownloadItem(magnet.TTH);

            if (di != null)
            {
                Engine.RemoveDownload(di);
            }

            var shared = Engine.Share.SearchByTth(magnet.TTH);

            if (shared != null)
            {
                Engine.Share.RemoveFile(magnet.TTH);
                File.Delete(shared.Value.SystemPath);
            }
        }

        public void Initialize()
        {
            var settings = EngineSettings.Default;

            settings.ActiveMode = Settings.ActiveMode;
            settings.UseSparseFiles = true;
            settings.AutoSelectPort = true;

            if (Settings.TCPPort != 0)
                settings.TcpPort = Settings.TCPPort;

            if (Settings.UDPPort != 0)
                settings.UdpPort = Settings.UDPPort;

            _engine = new DcEngine(settings);
            _engine.TagInfo.Version = "livedc";

            _hubManager = new DcHubManager(this, _client);

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

            Settings.Nickname = "livedc" + Guid.NewGuid().ToString().GetMd5Hash().Substring(0, 8);

            _engine.ActiveStatusChanged += delegate { OnStatusChanged(); };

            _engine.StartAsync();
            _engine.Connect();
        }

        public void Dispose()
        {
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
        }

        private void PortCheckComplete(CheckIpResult e)
        {
            logger.Info("Check port: {0} IsOpen:{1}", e.ExternalIpAddress, e.IsPortOpen);

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
    }
}

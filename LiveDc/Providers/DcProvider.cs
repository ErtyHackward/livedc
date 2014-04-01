using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
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
            if (Settings.AssocMagnetLinks)
            {
                if (!WindowsHelper.IsMagnetHandlerAssigned)
                {
                    WindowsHelper.RegisterMagnetHandler();
                }
            }
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
            if (!CanHandle(magnet))
                return null;

            return _engine.GetStream(magnet);
        }

        public bool CanHandle(Magnet magnet)
        {
            return !string.IsNullOrEmpty(magnet.TTH);
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
            control.Progress = GetMagnetCacheProgress(control.Magnet);

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

        public long ReleaseFileCache(long releaseBytes)
        {
            logger.Info("Requested relese of {0} bytes ", releaseBytes );
            var released = 0L;

            var list = _engine.Share.OldestItems().TakeWhile( i => (released += i.Magnet.Size) < releaseBytes ).ToList();

            foreach (var contentItem in list)
            {
                logger.Info("Deleting {0}", contentItem.SystemPath);
                _engine.Share.RemoveFile(contentItem.Magnet.TTH);
            }

            return released;
        }

        public float GetMagnetCacheProgress(Magnet magnet)
        {
            var di = Engine.DownloadManager.GetDownloadItem(magnet.TTH);

            if (di != null)
            {
                return (float)Engine.DownloadManager.GetTotalDownloadBytes(di) / magnet.Size;
            }
            else
            {
                var result = Engine.Share.SearchByTth(magnet.TTH);
                return result == null ? 0f : 1f;
            }
        }

        public long GetTotalUploadSpeed()
        {
            return Engine.TransferManager.Transfers().Uploads().UploadSpeed();
        }

        public long GetTotalDownloadSpeed()
        {
            return Engine.TransferManager.Transfers().Downloads().DownloadSpeed();
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
            settings.ReconnectTimeout = 45;

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
                    _engine.Share.Reload();
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

            if (!string.IsNullOrEmpty(Settings.PortCheckUrl))
                LiveApi.PortCheckUri = Settings.PortCheckUrl;

            ThreadPool.QueueUserWorkItem(o => AutoConfiguration());
        }

        private bool _providerConfiguration;

        public void AutoConfiguration()
        {
            logger.Info("Autoconfiguration started...");

            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;

                var config = new ProviderConfiguration();
                _providerConfiguration = false;

                try
                {
                    logger.Info("Loading provider specific data http://dc.local/ISP_favorites.xml");
                    var xmlData = wc.DownloadString("http://dc.local/ISP_favorites.xml");
                    var xml = new XmlSerializer(typeof(ProviderConfiguration));
                    logger.Info("Deserialization...");
                    config = (ProviderConfiguration)xml.Deserialize(new StringReader(xmlData));
                    _providerConfiguration = true;
                    logger.Info("Provider configuration sucessfully loaded");
                }
                catch (Exception e)
                {
                    logger.Error("Unable to load provider configuration: {0}", e.Message);
                }

                if (_providerConfiguration)
                {
                    if (!string.IsNullOrEmpty(config.LiveDcConfig.PortCheckUrl))
                    {
                        logger.Info("Using new port check url: {0}", config.LiveDcConfig.PortCheckUrl);
                        LiveApi.PortCheckUri  = config.LiveDcConfig.PortCheckUrl;
                        Settings.PortCheckUrl = config.LiveDcConfig.PortCheckUrl;
                        Settings.Save();
                    }

                    if (config.Hubs != null && config.Hubs.Count > 0)
                    {
                        logger.Info("Using provider hubs...");
                        Engine.Hubs.Clear();

                        foreach (var hubInfo in config.Hubs)
                        {
                            _hubManager.AddHub(hubInfo.Address);    
                        }
                        
                        Settings.Hubs = string.Join(";", config.Hubs.Select(h => DcHubManager.NormalizeHubAddress(h.Address)));
                        Settings.Save();
                    }
                }
            }

            LiveApi.CheckPortAsync(Settings.TCPPort, PortCheckComplete);
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
            
            if (string.IsNullOrEmpty(Settings.Hubs) || (DateTime.Now - Settings.LastHubCheck).TotalDays > 7)
            {
                if (!_providerConfiguration)
                {
                    _hubManager.FindHubs(IPAddress.Parse(e.ExternalIpAddress));
                }
            }

        }
    }
}

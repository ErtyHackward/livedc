using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Notify;
using LiveDc.Utilites;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;
using SharpDc.Structs;
using EngineSettings = MonoTorrent.Client.EngineSettings;

namespace LiveDc.Providers
{
    public class TorrentProvider : IP2PProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LiveClient _client;
        private readonly List<TorrentManager> _torrents = new List<TorrentManager>();
        private ClientEngine _engine;
        private TorrentSettings _torrentDefaults;

        private string TorrentFastResumePath { get { return Path.Combine(Settings.SettingsFolder, "fastresume.data"); } }
        private string TorrentDhtNodesPath { get { return Path.Combine(Settings.SettingsFolder, "dhtnodes.data"); } }
        public string TorrentsFolder { get { return Path.Combine(Settings.SettingsFolder, "Torrents"); } }
        
        public event EventHandler StatusChanged;

        /// <summary>
        /// Not applicable for torrents, always returns true
        /// </summary>
        public bool Online { get { return true; } }

        public List<TorrentManager> Torrents { get { return _torrents; } }

        public LiveClient Client { get { return _client; } }

        public ClientEngine Engine { get { return _engine; } }

        public TorrentSettings TorrentDefaults
        {
            get { return _torrentDefaults; }
        }

        public TorrentProvider(LiveClient client)
        {
            _client = client;

            string programName;
            string programPath;

            WindowsHelper.GetProgramAssociatedWithExt(false, ".torrent", out programName, out programPath);

            if (programPath != Application.ExecutablePath)
            {
                WindowsHelper.RegisterExtension(false, "LiveDC", ".torrent");
            }

            WindowsHelper.GetProgramAssociatedWithExt(true, ".torrent", out programName, out programPath);

            if (programPath != Application.ExecutablePath && VistaSecurity.IsAdmin())
            {
                WindowsHelper.RegisterExtension(true, "LiveDC", ".torrent");
            }
        }

        public void Initialize()
        {
            string downloadsPath = Client.Settings.StorageAutoSelect ? StorageHelper.GetBestSaveDirectory() : Client.Settings.StoragePath;


            var engineSettings = new EngineSettings(downloadsPath, Client.Settings.TorrentTcpPort)
                                     {
                                         PreferEncryption = false,
                                         AllowedEncryption = EncryptionTypes.All
                                     };

            _torrentDefaults = new TorrentSettings(4, 30, 0, 0);

            _engine = new ClientEngine(engineSettings);
            _engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, Client.Settings.TorrentTcpPort));
            byte[] nodes = null;
            try
            {
                nodes = File.ReadAllBytes(TorrentDhtNodesPath);
            }
            catch
            {
                logger.Info("No existing dht nodes could be loaded");
            }

            var dhtListner = new DhtListener(new IPEndPoint(IPAddress.Any, Client.Settings.TorrentTcpPort));
            var dht = new DhtEngine(dhtListner);
            _engine.RegisterDht(dht);
            dhtListner.Start();
            _engine.DhtEngine.Start(nodes);

            // If the SavePath does not exist, we want to create it.
            if (!Directory.Exists(_engine.Settings.SavePath))
                Directory.CreateDirectory(_engine.Settings.SavePath);

            // If the torrentsPath does not exist, we want to create it
            if (!Directory.Exists(TorrentsFolder))
                Directory.CreateDirectory(TorrentsFolder);

            BEncodedDictionary fastResume;
            try
            {
                fastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(TorrentFastResumePath));
            }
            catch
            {
                fastResume = new BEncodedDictionary();
            }

            foreach (var file in Directory.GetFiles(TorrentsFolder))
            {
                if (file.EndsWith(".torrent"))
                {
                    Torrent torrent;
                    try
                    {
                        torrent = Torrent.Load(file);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Couldn't decode {0}: ", file);
                        logger.Error(e.Message);
                        continue;
                    }

                    var manager = new TorrentManager(torrent, downloadsPath, TorrentDefaults);
                    if (fastResume.ContainsKey(torrent.InfoHash.ToHex()))
                        manager.LoadFastResume(new FastResume((BEncodedDictionary)fastResume[torrent.InfoHash.ToHex()]));
                    _engine.Register(manager);

                    _torrents.Add(manager);
                }
            }
        }

        public IEnumerable<Magnet> AllMagnets()
        {
            foreach (var torrentManager in _torrents)
            {
                foreach (var torrentFile in torrentManager.Torrent.Files.Where(f => f.Priority != Priority.DoNotDownload))
                {
                    yield return new Magnet { 
                        FileName = torrentFile.Path, 
                        Size = torrentFile.Length,
                        Trackers = torrentManager.Torrent.AnnounceUrls.SelectMany(t => t.AsEnumerable()).ToArray(),
                        BTIH = torrentManager.InfoHash.ToHex(),
                    };
                }
            }
        }

        private TorrentManager FindByMagnet(Magnet magnet)
        {
            var infoHash = magnet.ToInfoHash();
            return _torrents.FirstOrDefault(t => t.InfoHash == infoHash);
        }

        public Stream GetStream(Magnet magnet)
        {
            return new TorrentStream(FindByMagnet(magnet), magnet.FileName);
        }

        public bool CanHandle(Magnet magnet)
        {
            return !string.IsNullOrEmpty(magnet.SHA1) || !string.IsNullOrEmpty(magnet.BTIH);
        }

        public IStartItem StartItem(Magnet magnet)
        {
            return new TorrentStartItem(this, magnet);
        }

        public IStartItem StartItem(Torrent torrent)
        {
            return new TorrentStartItem(this, new Magnet { BTIH = torrent.InfoHash.ToHex()}, torrent);
        }

        public string GetVirtualPath(Magnet magnet)
        {
            return Path.Combine(Client.Drive.DriveRoot, Path.GetFileName(magnet.FileName));
        }

        public string GetRealPath(Magnet magnet)
        {
            var manager = FindByMagnet(magnet);
            return Path.Combine(manager.SavePath, magnet.FileName);
        }

        public void UpdateFileItem(DcFileControl control)
        {
            var manager = FindByMagnet(control.Magnet);
            if (manager != null)
            {
                var file = manager.Torrent.Files.First(f => f.Path == control.Magnet.FileName);

                control.DownloadSpeed = manager.Monitor.DownloadSpeed;
                control.DownloadedBytes = file.BytesDownloaded;
            }
        }

        public void DeleteFile(Magnet magnet)
        {
            var manager = FindByMagnet(magnet);
            var file = manager.Torrent.Files.First(f => f.Path == magnet.FileName);

            file.Priority = Priority.DoNotDownload;
            manager.Engine.DiskManager.Flush(manager);
            File.Delete(file.FullPath);

            if (manager.Torrent.Files.All(f => f.Priority == Priority.DoNotDownload))
            {
                manager.Dispose();
                _torrents.Remove(manager);
            }
        }

        public void Dispose()
        {
            logger.Info("Disposing torrents provider");
            var fastResume = new BEncodedDictionary();
            foreach (var t in _torrents)
            {
                t.Stop();
                while (t.State != TorrentState.Stopped)
                {
                    logger.Info("{0} is {1}", t.Torrent.Name, t.State);
                    Thread.Sleep(250);
                }

                fastResume.Add(t.Torrent.InfoHash.ToHex(), t.SaveFastResume().Encode());
            }
            
            File.WriteAllBytes(TorrentDhtNodesPath, _engine.DhtEngine.SaveNodes());
            File.WriteAllBytes(TorrentFastResumePath, fastResume.Encode());
            _engine.Dispose();

            logger.Info("Torrents provider is disposed");
        }
    }

    public static class MagnetExtensions
    {
        public static InfoHash ToInfoHash(this Magnet magnet)
        {
            if (string.IsNullOrEmpty(magnet.BTIH))
                throw new InvalidDataException("Unable to create InfoHash. Expected bit torrent info hash but null or empty");

            if (magnet.BTIH.Length == 40)
                return InfoHash.FromHex(magnet.BTIH);

            if (magnet.BTIH.Length == 32)
                return InfoHash.FromBase32(magnet.BTIH);

            throw new InvalidDataException("Unknown type of hash provided. Expected BTIH of length 32 or 40 chars");
        }
    }
}
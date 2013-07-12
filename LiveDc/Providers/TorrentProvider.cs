using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using LiveDc.Helpers;
using LiveDc.Notify;
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
        private readonly LiveClient _client;

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private List<TorrentManager> _torrents = new List<TorrentManager>();
        private ClientEngine _engine;

        private string TorrentFastResumePath { get { return Path.Combine(Settings.SettingsFolder, "fastresume.data"); } }
        private string TorrentDhtNodesPath { get { return Path.Combine(Settings.SettingsFolder, "dhtnodes.data"); } }
        private string TorrentsFolder { get { return Path.Combine(Settings.SettingsFolder, "Torrents"); } }
        
        public event EventHandler StatusChanged;

        /// <summary>
        /// Not applicable for torrents, always returns true
        /// </summary>
        public bool Online { get { return true; } }

        public TorrentProvider(LiveClient client)
        {
            _client = client;
        }

        public void Initialize()
        {
            string downloadsPath = _client.Settings.StorageAutoSelect ? StorageHelper.GetBestSaveDirectory() : _client.Settings.StoragePath;


            var engineSettings = new EngineSettings(downloadsPath, _client.Settings.TorrentTcpPort)
                                     {
                                         PreferEncryption = false,
                                         AllowedEncryption = EncryptionTypes.All
                                     };

            var torrentDefaults = new TorrentSettings(4, 30, 0, 0);

            _engine = new ClientEngine(engineSettings);
            _engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, _client.Settings.TorrentTcpPort));
            byte[] nodes = null;
            try
            {
                nodes = File.ReadAllBytes(TorrentDhtNodesPath);
            }
            catch
            {
                logger.Info("No existing dht nodes could be loaded");
            }

            var dhtListner = new DhtListener(new IPEndPoint(IPAddress.Any, _client.Settings.TorrentTcpPort));
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

                    var manager = new TorrentManager(torrent, downloadsPath, torrentDefaults);
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
                        BTIH = torrentManager.InfoHash.ToString(),
                    };
                }
            }
        }

        public Stream GetStream(Magnet magnet)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle(Magnet magnet)
        {
            return !string.IsNullOrEmpty(magnet.SHA1) || !string.IsNullOrEmpty(magnet.BTIH);
        }

        public IStartItem StartItem(Magnet magnet)
        {
            throw new NotImplementedException();
        }

        public string GetVirtualPath(Magnet magnet)
        {
            throw new NotImplementedException();
        }

        public string GetRealPath(Magnet magnet)
        {
            throw new NotImplementedException();
        }

        public void UpdateFileItem(DcFileControl control)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(Magnet magnet)
        {
            throw new NotImplementedException();
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
}
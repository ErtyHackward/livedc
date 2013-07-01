using System;
using System.Collections.Generic;
using System.IO;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public class TorrentProvider : IP2PProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string TorrentFastResumePath { get { return Path.Combine(Settings.SettingsFolder, "fastresume.data"); } }
        private string TorrentDhtNodesPath { get { return Path.Combine(Settings.SettingsFolder, "dhtnodes.data"); } }
        private string TorrentsFolder { get { return Path.Combine(Settings.SettingsFolder, "Torrents"); } }
        
        public event EventHandler StatusChanged;

        /// <summary>
        /// Not applicable for torrents, always returns false
        /// </summary>
        public bool Online { get { return false; } }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Magnet> AllMagnets()
        {
            throw new NotImplementedException();
        }

        public Stream GetStream(Magnet magnet)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle(Magnet magnet)
        {
            throw new NotImplementedException();
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
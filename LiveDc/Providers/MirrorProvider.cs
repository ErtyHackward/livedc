using System.Collections.Generic;
using System.IO;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public class MirrorProvider : IFsProvider
    {
        private readonly LiveClient _client;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<Magnet> _registeredFiles = new List<Magnet>();

        public MirrorProvider(LiveClient client)
        {
            _client = client;
        }

        public IEnumerable<Magnet> AllMagnets()
        {
            return _registeredFiles;
        }
        
        public Stream GetStream(Magnet magnet)
        {
            if (magnet.WebSources == null || !File.Exists(magnet.WebSources[0]))
                return null;

            var cache = new BufferedStream(File.OpenRead(magnet.WebSources[0]), 1024 * 128);
            return cache;
        }

        /// <summary>
        /// Tries to request file length and map the file
        /// </summary>
        /// <param name="localPath"></param>
        public Magnet RegisterFile(string localPath)
        {
            var fi = new FileInfo(localPath);

            var magnet = new Magnet { FileName = fi.Name, Size = fi.Length, WebSources = new[] { localPath } };

            _registeredFiles.Add(magnet);

            logger.Info("Mirror file registered {0}", magnet);

            return magnet;
        }
        
        public bool CanHandle(Magnet magnet)
        {
            return magnet.WebSources != null && File.Exists(magnet.WebSources[0]);
        }

        public IStartItem StartItem(Magnet magnet)
        {
            return new SimpleStartItem(magnet, _client.Drive);
        }
    }
}
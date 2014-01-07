using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpDc;
using SharpDc.Helpers;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    /// <summary>
    /// Allows to map http files to local virtual drive
    /// </summary>
    public class HttpProvider : IFsProvider
    {
        private readonly LiveClient _client;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<Magnet> _registeredFiles = new List<Magnet>();

        public HttpProvider(LiveClient client)
        {
            _client = client;
        }

        public IEnumerable<Magnet> AllMagnets()
        {
            return _registeredFiles;
        }

        public Stream GetStream(Magnet magnet)
        {
            if (magnet.WebSources == null || !magnet.WebSources[0].StartsWith("http"))
                return null;

            var cache = new BufferedStream(new HttpFileStream(magnet.WebSources[0], magnet.Size), 1024 * 128);
            return cache;
        }

        /// <summary>
        /// Tries to request file length and map the file
        /// </summary>
        /// <param name="httpUrl"></param>
        public Magnet RegisterFile(string httpUrl)
        {
            var alreadyRegistered = _registeredFiles.FirstOrDefault(m => m.WebSources != null && m.WebSources[0] == httpUrl);

            if (alreadyRegistered.WebSources != null)
            {
                return alreadyRegistered;
            }

            Exception x;
            long size;
            string fileName;
            HttpHelper.GetFileNameAndSize(httpUrl, out fileName, out size, out x);
            if (x != null)
                throw x;

            if (fileName == null)
                fileName = Path.GetFileName(httpUrl);

            var qIndex = fileName.IndexOf('?');
            if (qIndex != -1)
            {
                fileName = fileName.Substring(0, qIndex);
            }

            var magnet = new Magnet { FileName = fileName, Size = size, WebSources = new[] { httpUrl } };

            _registeredFiles.Add(magnet);

            logger.Info("Http file registered {0}", magnet);

            return magnet;
        }

        public bool CanHandle(Magnet magnet)
        {
            return magnet.WebSources != null && magnet.WebSources[0].StartsWith("http");
        }

        public IStartItem StartItem(Magnet magnet)
        {
            return new SimpleStartItem(magnet, _client.Drive);
        }
    }
}

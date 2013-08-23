using NLog;
using SharpDc.Logging;

namespace LiveDc
{
    /// <summary>
    /// SharpDc to NLog manager
    /// </summary>
    internal class NLogManager : ILogManager
    {
        public ILogger GetLogger(string className)
        {
            return new NLogProxy(NLog.LogManager.GetLogger(className));
        }
    }

    internal class NLogProxy : ILogger
    {
        private readonly Logger _logger;

        public NLogProxy(Logger logger)
        {
            _logger = logger;
        }

        public void Info(string message, params object[] args)
        {
            _logger.Info(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            _logger.Warn(message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }
    }

    /// <summary>
    /// SharpDc to NLog manager
    /// </summary>
    internal class TorrentNLogManager : MonoTorrent.ILogManager
    {
        public MonoTorrent.ILogger GetLogger(string className)
        {
            return new TorrentNLogProxy(NLog.LogManager.GetLogger(className));
        }
    }

    internal class TorrentNLogProxy : MonoTorrent.ILogger
    {
        private readonly Logger _logger;

        public TorrentNLogProxy(Logger logger)
        {
            _logger = logger;
        }

        public void Info(string message, params object[] args)
        {
            _logger.Info(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            _logger.Warn(message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.Common;
using SharpDc;

namespace LiveDc.Providers
{
    public class TorrentStream : Stream
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly TorrentManager _torrentManager;
        
        private FileStream _innerStream;
        private TorrentFile _file;
        private int _readTimeout = 120000;

        public override int ReadTimeout
        {
            get { return _readTimeout; }
            set { _readTimeout = value; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _file.Length; }
        }

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        public TorrentStream(TorrentManager torrentManager, string filePathVirtual)
        {
            _torrentManager = torrentManager;
            _file = torrentManager.Torrent.Files.First(f => f.Path == filePathVirtual);
            SetupFile();
        }

        private void SetupFile()
        {
            if (_innerStream == null)
            {
                if (!File.Exists(_file.FullPath))
                {
                    var directory = Path.GetDirectoryName(_file.FullPath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.Create(_file.FullPath);

                    using (var fs = new FileStream(_file.FullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        fs.SetLength(_file.Length);
                    }
                }

                _innerStream = new FileStream(_file.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }
        
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int timeout = ReadTimeout;

            logger.Info("Try read torrent data {0} {1}", Utils.FormatBytes(_innerStream.Position), count);

            while (!AreaDownloaded(_innerStream.Position, count))
            {
                Thread.Sleep(100);
                if ((timeout -= 100) <= 0)
                    throw new TimeoutException(string.Format("Unable to read data in {0} secs", ReadTimeout / 1000));
            }
            
            logger.Info("Read torrent data {0} {1}", Utils.FormatBytes(_innerStream.Position), count);
            return _innerStream.Read(buffer, offset, count);
        }

        private bool AreaDownloaded(long position, int length)
        {
            var slidingPicker = _torrentManager.PieceManager.GetPicker<SlidingWindowPicker>();
            var startPiece = _file.StartPieceIndex + (int)((position + _file.StartPieceOffset) / _torrentManager.Torrent.PieceLength);
            slidingPicker.HighPrioritySetStart = startPiece;
            slidingPicker.HighPrioritySetSize = 3;
            var endPiece = _file.StartPieceIndex + (int)((position + _file.StartPieceOffset + length) / _torrentManager.Torrent.PieceLength);

            for (var i = startPiece; i <= endPiece; i++)
            {
                if (!_torrentManager.Bitfield[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _innerStream.Dispose();
            base.Dispose(disposing);
        }

    }
}
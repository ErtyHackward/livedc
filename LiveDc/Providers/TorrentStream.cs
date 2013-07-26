using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using LiveDc.Helpers;
using MonoTorrent.Client;
using MonoTorrent.Common;
using SharpDc;

namespace LiveDc.Providers
{
    /// <summary>
    /// Provides file stream to read data from the file that is downloading in the torrent right now
    /// Adjusts piece priority on each Read operation
    /// </summary>
    public class TorrentStream : Stream
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly TorrentManager _torrentManager;
        
        private FileStream _innerStream;
        private TorrentFile _file;
        private int _readTimeout = 120000;
        private Stopwatch _readStopwatch = new Stopwatch();

        /// <summary>
        /// Gets or sets a value, in miliseconds, that determines how long the stream will attempt to read before timing out. 
        /// </summary>
        /// <returns>
        /// A value, in miliseconds, that determines how long the stream will attempt to read before timing out.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="P:System.IO.Stream.ReadTimeout"/> method always throws an <see cref="T:System.InvalidOperationException"/>. </exception><filterpriority>2</filterpriority>
        public override int ReadTimeout
        {
            get { return _readTimeout; }
            set { _readTimeout = value; }
        }

        /// <summary>
        /// Always returns true
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Always returns true
        /// </summary>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Always returns false
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the length in bytes of the file.
        /// </summary>
        /// <returns>
        /// A long value representing the length of the stream in bytes.
        /// </returns>
        public override long Length
        {
            get { return _file.Length; }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <returns>
        /// The current position within the stream.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
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

        /// <summary>
        /// throws NotSupportedException
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter. </param><param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position. </param><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        /// <summary>
        /// throws NotSupportedException
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// Also adjusts torrent piece priorities to obtain the data as fast as possible
        /// </summary>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream. </param><param name="count">The maximum number of bytes to be read from the current stream. </param><exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. </exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support reading. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override int Read(byte[] buffer, int offset, int count)
        {
            _readStopwatch.Restart();

            logger.Info("Requested torrent data at: {0} len: {1}", Utils.FormatBytes(_innerStream.Position), count);

            while (!AreaDownloaded(_innerStream.Position, count))
            {
                Thread.Sleep(100);
                if (_readStopwatch.Elapsed.Seconds > ReadTimeout)
                    throw new TimeoutException(string.Format("Unable to read data in {0}", ReadTimeout / 1000));
            }
            
            logger.Info("Read torrent data at: {0} len: {1} in {2}", Utils.FormatBytes(_innerStream.Position), count, _readStopwatch.Elapsed.FormatInterval());
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

        /// <summary>
        /// throws NotSupportedException
        /// </summary>
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
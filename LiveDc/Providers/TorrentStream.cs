using System;
using System.IO;
using System.Linq;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace LiveDc.Providers
{
    public class TorrentStream : Stream
    {
        private readonly TorrentManager _torrentManager;
        
        private FileStream _innerStream;
        private TorrentFile _file;

        public TorrentStream(TorrentManager torrentManager, string filePathVirtual)
        {
            _torrentManager = torrentManager;
            _file = torrentManager.Torrent.Files.First(f => f.Path == filePathVirtual);            
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
            int timeout = 30000;

            while (!AreaDownload(_innerStream.Position, count))
            {
                Thread.Sleep(100);
                if ((timeout -= 100) <= 0)
                    throw new TimeoutException("Unable to read data in 30 secs");
            }

            return _innerStream.Read(buffer, offset, count);
        }

        private bool AreaDownload(long position, int length)
        {
            var slidingPicker = (SlidingWindowPicker)_torrentManager.PieceManager.Picker;
            var startPiece = (int)(position / _torrentManager.Torrent.PieceLength);
            slidingPicker.HighPrioritySetStart = startPiece;
            var endPiece = (int)((position + length) / _torrentManager.Torrent.PieceLength);

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
    }
}
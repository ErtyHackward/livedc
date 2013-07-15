using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using LiveDc.Helpers;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Common;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public class TorrentStartItem : StartItem
    {
        private TorrentProvider _torrentProvider;
        private TorrentManager _torrent;
        private TorrentFile _file;
        
        public TorrentStartItem(TorrentProvider torrentProvider, Magnet magnet)
        {
            Progress = float.PositiveInfinity;
            _torrentProvider = torrentProvider;
            Magnet = magnet;
            var ml = new MagnetLink(magnet.ToString());

            // find or create torrent
            _torrent = _torrentProvider.Torrents.FirstOrDefault(t => t.InfoHash == ml.InfoHash);
            if (_torrent == null)
            {
                _torrent = new TorrentManager(ml, StorageHelper.GetBestSaveDirectory(), _torrentProvider.TorrentDefaults, _torrentProvider.TorrentsFolder);
            }
            
            // update priorities
            foreach (var torrentFile in _torrent.Torrent.Files)
            {
                if (torrentFile.Path == magnet.FileName)
                {
                    torrentFile.Priority = Priority.Immediate;
                    _file = torrentFile;
                }
                else
                    torrentFile.Priority = Priority.DoNotDownload;
            }

            // check if the file is downloaded completely
            var doneSegments = _torrent.Bitfield.Clone();
            if (doneSegments.And(_file.BitField).TrueCount == _file.BitField.Length)
            {
                ReadyToStart = true;
                ShellHelper.Start(_file.FullPath);
                return;
            }

            _torrentProvider.Client.History.AddItem(magnet);

            new ThreadStart(FormThread).BeginInvoke(null, null);
        }

        private void FormThread()
        {
            var sw = Stopwatch.StartNew();

            while (_file.BytesDownloaded < 1024 * 1024 && sw.Elapsed.TotalSeconds < 30 && UserWaits())
            {
                StatusMessage = string.Format("Поиск источников... ({0})", _torrent.Peers.Available);
                Thread.Sleep(500);
            }

            if (_file.BytesDownloaded >= 1024 * 1024)
            {
                StartIn5Seconds();
                return;
            }

            StatusMessage = "Не удается начать загрузку. Источники: " + _torrent.Peers.Available;
            Progress = float.NaN;

            while (!_cancel && !_started)
            {
                if (_file.BytesDownloaded != 0)
                {
                    ReadyToStart = true;
                    StatusMessage = "Файл готов к работе";
                }

                Thread.Sleep(100);
            }

            if (_cancel && !_addToQueue)
            {
                _torrentProvider.DeleteFile(Magnet);
            }
        }

        public override void OpenFile()
        {
            if (_started || _addToQueue)
                return;

            _started = true;

            StatusMessage = "Открываю файл...";
            ShellHelper.Start(Path.Combine(_torrentProvider.Client.Drive.DriveRoot, _file.Path));
        }
    }
}
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
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
        
        public TorrentStartItem(TorrentProvider torrentProvider, Magnet magnet, Torrent torrent = null)
        {
            Progress = float.PositiveInfinity;
            _torrentProvider = torrentProvider;
            Magnet = magnet;
            var ml = new MagnetLink(magnet.ToString());

            // find or create torrent
            _torrent = _torrentProvider.Torrents.FirstOrDefault(t => t.InfoHash == ml.InfoHash);
            if (_torrent == null)
            {
                if (torrent != null)
                {
                    _torrent = new TorrentManager(torrent, StorageHelper.GetBestSaveDirectory(), _torrentProvider.TorrentDefaults);

                    foreach (var torrentFile in _torrent.Torrent.Files)
                    {
                        torrentFile.Priority = Priority.DoNotDownload;
                    }
                }
                else
                    _torrent = new TorrentManager(ml, StorageHelper.GetBestSaveDirectory(), _torrentProvider.TorrentDefaults, _torrentProvider.TorrentsFolder);

                _torrentProvider.RegisterTorrent(_torrent);
                _torrent.Start();
            }
            else
            {
                _file = _torrent.Torrent.Files.First(f => f.Path == magnet.FileName);

                // check if the file is downloaded completely
                if (_file.BitField.TrueCount == _file.BitField.Length)
                {
                    Closed = true;
                    ShellHelper.Start(_file.FullPath);
                    return;
                }
            }
            
            new ThreadStart(FormThread).BeginInvoke(null, null);
        }

        private void FormThread()
        {
            while (_torrent.Torrent == null)
            {
                // we have created the torrent from the InfoHash, to continue we need to receive torrent file first
                StatusMessage = "Поиск сведений о загрузке";

                if (!UserWaits())
                    return;

                Thread.Sleep(100);
            }

            // update priorities
            foreach (var torrentFile in _torrent.Torrent.Files)
            {
                if (!string.IsNullOrEmpty(Magnet.FileName) && torrentFile.Path == Magnet.FileName)
                {
                    torrentFile.Priority = Priority.Immediate;
                    _file = torrentFile;
                }
                else
                    torrentFile.Priority = Priority.DoNotDownload;
            }


            bool updateMagnetFileName = false;
            if (_file == null)
            {
                if (_torrent.Torrent.Files.Length == 1)
                {
                    _file = _torrent.Torrent.Files[0];
                    updateMagnetFileName = true;
                }
                else
                {
                    var form = new FrmTorrentFiles(_torrent.Torrent);

                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        _file = form.SelectedFile;
                        updateMagnetFileName = true;
                    }
                    else
                    {
                        Progress = float.NaN;
                        StatusMessage = "Операция отменена";
                        Closed = true;
                        return;
                    }
                }
            }

            if (updateMagnetFileName || string.IsNullOrEmpty(Magnet.FileName) || Magnet.Size == 0)
            {
                var magnet = Magnet;
                magnet.FileName = _file.Path;
                magnet.Size = _file.Length;
                Magnet = magnet;
            }

            _torrentProvider.Client.History.AddItem(Magnet);

            _file.Priority = Priority.Immediate;
            
            var sw = Stopwatch.StartNew();

            while (_file.BytesDownloaded < 1024 * 1024 && sw.Elapsed.TotalSeconds < 120 && UserWaits())
            {
                StatusMessage = string.Format("Поиск источников... ({0})", _torrent.OpenConnections);
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
﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Common;
using SharpDc;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public class TorrentStartItem : StartItem
    {
        private TorrentProvider _torrentProvider;
        private TorrentManager _manager;
        private TorrentFile _file;
        private FrmTorrentFiles _filesForm;

        public TorrentStartItem(TorrentProvider torrentProvider, Magnet magnet, Torrent torrent = null)
        {
            Progress = float.PositiveInfinity;
            _torrentProvider = torrentProvider;
            Magnet = magnet;
            var ml = new MagnetLink(magnet.ToString());

            // find or create torrent
            _manager = _torrentProvider.Torrents.FirstOrDefault(t => t.InfoHash == ml.InfoHash);
            if (_manager == null)
            {
                if (torrent != null)
                {
                    _manager = new TorrentManager(torrent, StorageHelper.GetBestSaveDirectory(), _torrentProvider.TorrentDefaults);

                    foreach (var torrentFile in _manager.Torrent.Files)
                    {
                        torrentFile.Priority = Priority.DoNotDownload;
                    }
                }
                else
                    _manager = new TorrentManager(ml, StorageHelper.GetBestSaveDirectory(), _torrentProvider.TorrentDefaults, _torrentProvider.TorrentsFolder);

                _torrentProvider.RegisterTorrent(_manager);
                _manager.Start();
            }
            else
            {
                if (!string.IsNullOrEmpty(magnet.FileName))
                {
                    _file = _manager.Torrent.Files.FirstOrDefault(f => f.Path == magnet.FileName);
                    
                    // check if the file is downloaded completely
                    if (_file != null && _file.BitField.TrueCount == _file.BitField.Length)
                    {
                        Closed = true;
                        ShellHelper.Start(_file.FullPath);
                        return;
                    }
                }
            }
            
            new ThreadStart(FormThread).BeginInvoke(null, null);
        }

        private void FormThread()
        {
            while (!_manager.HasMetadata)
            {
                // we have created the torrent from the InfoHash, to continue we need to receive torrent file first
                StatusMessage = "Поиск метаданных...";

                if (!UserWaits())
                {
                    _torrentProvider.CancelTorrent(_manager);
                    return;
                }

                Thread.Sleep(100);
            }

            // update priorities
            foreach (var torrentFile in _manager.Torrent.Files)
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
                if (_manager.Torrent.Files.Length == 1)
                {
                    _file = _manager.Torrent.Files[0];
                    updateMagnetFileName = true;
                }
                else
                {
                    StatusMessage = "Выбор файла...";
                    while (_file == null)
                    {
                        Thread.Sleep(100);

                        if (Closed)
                        {
                            _torrentProvider.CancelTorrent(_manager);
                            return;
                        }
                    }
                    
                    updateMagnetFileName = true;
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
                StatusMessage = string.Format("Загрузка... {0}", _manager.Monitor.DownloadSpeed != 0 ? Utils.FormatBytes(_manager.Monitor.DownloadSpeed) + "/c" : "");
                Thread.Sleep(500);
            }

            if (_file.BytesDownloaded >= 1024 * 1024)
            {
                StartIn5Seconds();
                return;
            }

            StatusMessage = "Не удается начать загрузку. Источники: " + _manager.Peers.Available;
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
                _torrentProvider.Client.History.DeleteItem(Magnet);

                // warn: this could take long time to finish (minute or so)
                _torrentProvider.DeleteFile(Magnet);
            }
        }

        public override void MainThreadAction(Form active)
        {
            if (_filesForm != null)
                return;

            if (_file == null && _manager != null && _manager.HasMetadata && _manager.Torrent.Files.Length > 1)
            {
                _filesForm = new FrmTorrentFiles(_manager.Torrent);

                if (_filesForm.ShowDialog(active) == DialogResult.OK)
                {
                    _file = _filesForm.SelectedFile;
                }
                else
                {
                    Progress = float.NaN;
                    StatusMessage = "Операция отменена";
                    Closed = true;
                }

                _filesForm = null;
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
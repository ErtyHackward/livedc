using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Providers;
using SharpDc.Interfaces;
using SharpDc.Structs;

namespace LiveDc
{
    /// <summary>
    /// Handles DC file download startup
    /// </summary>
    public class DcStartItem : StartItem
    {
        private readonly DcProvider _provider;
        private DownloadItem _currentDownload;
        
        public DcStartItem(Magnet magnet, DcProvider provider)
        {
            Progress = float.PositiveInfinity;
            Magnet = magnet;
            _provider = provider;
            var existingItem = _provider.Engine.Share.SearchByTth(magnet.TTH);

            if (existingItem != null)
            {
                Closed = true;
                ShellHelper.Start(existingItem.Value.SystemPath);
                return;
            }

            if (_provider.HubManager.InitializationCompleted && !_provider.Engine.Active)
            {
                MessageBox.Show("Не удалось установить соединение ни с одним из хабов. Проверьте сетевое подключение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var item = _provider.Engine.DownloadManager.GetDownloadItem(magnet.TTH);

            if (_currentDownload != null && _currentDownload != item)
                _provider.Engine.PauseDownload(_currentDownload);

            if (item != null)
            {
                if (item.Priority == DownloadPriority.Pause)
                    item.Priority = DownloadPriority.Normal;

                _currentDownload = item;
                Closed = true;
                ShellHelper.Start(Path.Combine(_provider.LiveClient.Drive.DriveRoot, _currentDownload.Magnet.FileName));
                return;
            }

            _provider.LiveClient.History.AddItem(magnet);

            _currentDownload = _provider.Engine.DownloadFile(magnet);
            _currentDownload.LogSegmentEvents = true;

            new ThreadStart(FormThread).BeginInvoke(null, null);
        }

        private void FormThread()
        {
            while (!_provider.HubManager.InitializationCompleted && !_cancel)
            {
                StatusMessage = "Подключение к хабам...";
                Thread.Sleep(100);
            }

            if (_currentDownload.Sources.Count == 0)
            {
                while (UserWaits() && _provider.Engine.SearchManager.EstimateSearch(_currentDownload) != TimeSpan.MaxValue)
                {
                    StatusMessage = string.Format("Поиск через {0} сек", (int)_provider.Engine.SearchManager.EstimateSearch(_currentDownload).TotalSeconds);
                    Thread.Sleep(100);
                }
            }

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.Seconds < 20 && UserWaits())
            {
                if (_currentDownload.DoneSegmentsCount == 0)
                {
                    StatusMessage = string.Format("Идет поиск. Найдено {0} источников", _currentDownload.Sources.Count);
                }
                else
                {
                    StartIn5Seconds();
                    break;
                }

                Thread.Sleep(100);
            }

            if (_currentDownload.DoneSegmentsCount == 0)
            {
                Progress = float.NaN;
                StatusMessage = string.Format("Не удается начать загрузку. Источников {0}.", _currentDownload.Sources.Count);
            }

            while (!_cancel && !_started)
            {
                if (_currentDownload.DoneSegmentsCount != 0)
                {
                    ReadyToStart = true;
                    StatusMessage = "Файл готов к работе";
                }

                Thread.Sleep(100);
            }

            if (_cancel && !_addToQueue)
            {
                _provider.Engine.RemoveDownload(_currentDownload);
                _currentDownload = null;
                _provider.LiveClient.History.DeleteItem(Magnet);
            }
        }

        public override void OpenFile()
        {
            if (_started || _addToQueue)
                return;

            _started = true;

            StatusMessage = "Открываю файл...";
            ShellHelper.Start(Path.Combine(_provider.LiveClient.Drive.DriveRoot, _currentDownload.Magnet.FileName));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Managers;
using LiveDc.Providers;
using SharpDc.Interfaces;
using SharpDc.Structs;

namespace LiveDc
{
    /// <summary>
    /// Describes GUI-protocol interaction object at the stage of file bootstap
    /// </summary>
    public interface IStartItem : IDisposable
    {
        Magnet Magnet { get; }

        /// <summary>
        /// Indicates if file data is available
        /// </summary>
        bool ReadyToStart { get; }

        /// <summary>
        /// Gets current status message
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// Puts this item to be downloaded in background
        /// Dispose will no longer stop download of the item
        /// </summary>
        void AddToQueue();
    }

    public class DcStartItem : IStartItem
    {
        private readonly Magnet _magnet;
        private readonly DcProvider _provider;
        private DownloadItem _currentDownload;

        public DcStartItem(Magnet magnet, DcProvider provider)
        {
            _magnet = magnet;
            _provider = provider;
            var existingItem = _provider.Engine.Share.SearchByTth(magnet.TTH);

            if (existingItem != null)
            {
                Process.Start(existingItem.Value.SystemPath);
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

                ShellHelper.Start(Path.Combine(_liveClient.Drive.DriveRoot, _currentDownload.Magnet.FileName));
                return;
            }

            _liveClient.History.AddItem(magnet);

            _currentDownload = _provider.Engine.DownloadFile(magnet);
            _currentDownload.LogSegmentEvents = true;

            new ThreadStart(FormThread).BeginInvoke(null, null);
        }

        private void FormThread()
        {
            _liveClient.AsyncOperation.Post((o) => _statusForm.UpdateAndShow(), null);

            while (!_liveClient.HubManager.InitializationCompleted && !_cancel)
            {
                UpdateMessage("Подключение к хабам...");
                Thread.Sleep(100);
            }

            if (_currentDownload.Sources.Count == 0)
            {
                while (!_cancel && _liveClient.Engine.SearchManager.EstimateSearch(_currentDownload) != TimeSpan.MaxValue)
                {
                    UpdateMessage(string.Format("Поиск через {0} сек", (int)_liveClient.Engine.SearchManager.EstimateSearch(_currentDownload).TotalSeconds));
                    Thread.Sleep(100);
                }
            }

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.Seconds < 20 && !_cancel)
            {
                if (_currentDownload.DoneSegmentsCount == 0)
                {
                    UpdateMessage(string.Format("Идет поиск. Найдено {0} источников", _currentDownload.Sources.Count));
                }
                else
                {
                    int timeout = 5;
                    while (timeout-- > 0)
                    {
                        UpdateMessage("Файл доступен. Запуск через " + timeout);
                        _liveClient.AsyncOperation.Post(o => { _statusForm.UpdateStartButton(timeout); _statusForm.progressBar.Style = ProgressBarStyle.Continuous; }, null);
                        Thread.Sleep(1000);
                    }
                    OpenFile();
                    break;
                }

                Thread.Sleep(100);
            }

            if (_currentDownload.DoneSegmentsCount == 0)
            {
                _liveClient.AsyncOperation.Post(o => _statusForm.progressBar.Enabled = false, null);
                UpdateMessage(string.Format("Не удается начать загрузку. Источников {0}.", _currentDownload.Sources.Count));
            }

            while (!_cancel && !_started)
            {
                if (_currentDownload.DoneSegmentsCount != 0)
                    _liveClient.AsyncOperation.Post(o => _statusForm.UpdateStartButton(), null);

                Thread.Sleep(100);
            }

            if (_cancel && !_addToQueue)
            {
                _liveClient.Engine.RemoveDownload(_currentDownload);
                _currentDownload = null;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Magnet Magnet { get { return _magnet; } }
        public bool ReadyToStart { get; private set; }
        public string StatusMessage { get; private set; }
    }

    public class TorrentStartItem : IStartItem
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Magnet Magnet { get; private set; }
        public bool ReadyToStart { get; private set; }
        public string StatusMessage { get; private set; }
    }
}

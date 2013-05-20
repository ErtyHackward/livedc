using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using SharpDc.Interfaces;
using SharpDc.Structs;

namespace LiveDc
{
    public class LaunchManager
    {
        private LiveClient _liveClient;
        private FrmStatus _statusForm;
        private DownloadItem _currentDownload;

        private bool _started;
        private bool _cancel;
        private bool _addToQueue;

        public Magnet Magnet { get; set; }

        public LiveClient Client { get { return _liveClient; } }

        public LaunchManager(LiveClient liveClient)
        {
            _liveClient = liveClient;
            _statusForm = new FrmStatus(this);
        }

        public void StartFile(Magnet magnet)
        {
            _cancel = false;
            _started = false;
            _addToQueue = false;

            Magnet = magnet;

            try
            {
                var existingItem = _liveClient.Engine.Share.SearchByTth(magnet.TTH);

                if (existingItem != null)
                {
                    Process.Start(existingItem.Value.SystemPath);
                    return;
                }

                if (_liveClient.HubManager.InitializationCompleted && !_liveClient.Engine.Active)
                {
                    MessageBox.Show("Не удалось установить соединение ни с одним из хабов. Проверьте сетевое подключение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_liveClient.Drive.IsReady)
                {
                    MessageBox.Show("Не удалось подключить виртуальный диск. Попробуйте перезагрузить компьютер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var item = _liveClient.Engine.DownloadManager.GetDownloadItem(magnet.TTH);

                if (_currentDownload != null && _currentDownload != item)
                    _liveClient.Engine.PauseDownload(_currentDownload);

                if (item != null)
                {
                    if (item.Priority == DownloadPriority.Pause)
                        item.Priority = DownloadPriority.Normal;

                    _currentDownload = item;

                    ShellHelper.Start(Path.Combine(_liveClient.Drive.DriveRoot, _currentDownload.Magnet.FileName));
                    return;
                }

                _liveClient.History.AddItem(magnet);
                
                _currentDownload = _liveClient.Engine.DownloadFile(magnet);
                _currentDownload.LogSegmentEvents = true;

                new ThreadStart(FormThread).BeginInvoke(null, null);
            }
            catch (Exception x)
            {
                MessageBox.Show("Не удается начать загрузку: " + x.Message);
            }
        }

        public void OpenFile()
        {
            if (_started || _addToQueue)
                return;

            _started = true;

            UpdateMessage("Открываю файл...");
            ShellHelper.Start(Path.Combine(_liveClient.Drive.DriveRoot, _currentDownload.Magnet.FileName));
            _liveClient.AsyncOperation.Post((o) => _statusForm.Hide(), null);
        }

        public void AddToQueue()
        {
            _addToQueue = true;
            _cancel = true;
        }

        public void Cancel()
        {
            _cancel = true;
        }
        
        private void FormThread()
        {
            _liveClient.AsyncOperation.Post((o) => _statusForm.UpdateAndShow(), null);

            var sw = Stopwatch.StartNew();

            while (!_liveClient.HubManager.InitializationCompleted && !_cancel)
            {
                UpdateMessage("Подключение к хабам...");
                Thread.Sleep(100);
            }

            if (_currentDownload.Sources.Count == 0)
            {
                while (!_cancel && _liveClient.Engine.SearchManager.CurrentSearch != null && _liveClient.Engine.SearchManager.CurrentSearch.Value.SearchRequest != _currentDownload.Magnet.TTH)
                {
                    UpdateMessage(string.Format("Поиск через {0} сек", (int)_liveClient.Engine.SearchManager.EstimateSearch(_currentDownload).TotalSeconds));
                    Thread.Sleep(100);
                }
            }

            while (sw.Elapsed.Seconds < 20 && !_cancel)
            {
                if (_currentDownload.DoneSegmentsCount == 0)
                {
                    UpdateMessage(string.Format("Идет поиск. Найдено {0} источников", _currentDownload.Sources.Count));
                }
                else
                {
                    int timeout = 5;

                    while (timeout-- >= 0)
                    {
                        _liveClient.AsyncOperation.Post(o => _statusForm.UpdateStartButton(timeout), null);
                        Thread.Sleep(1000);
                    }
                    OpenFile();
                    break;
                }

                Thread.Sleep(100);
            }

            if (_currentDownload.DoneSegmentsCount == 0)
            {
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

        private void UpdateMessage(string status)
        {
            _liveClient.AsyncOperation.Post((o) =>
            {
                _statusForm.statusLabel.Text = status;
            }, null);
        }
    }
}

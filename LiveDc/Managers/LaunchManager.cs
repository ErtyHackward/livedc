using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Helpers;
using SharpDc.Interfaces;
using SharpDc.Structs;

namespace LiveDc.Managers
{
    /// <summary>
    /// Responsible to tune engine to start file downloading
    /// </summary>
    public class LaunchManager
    {
        private LiveClient _liveClient;
        private FrmStatus _statusForm;
        

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

            if (!_liveClient.Drive.IsReady)
            {
                MessageBox.Show("Не удалось подключить виртуальный диск. Попробуйте перезагрузить компьютер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var provider = _liveClient.Providers.First(p => p.CanHandle(magnet));

                var item = provider.StartItem(magnet);

                _statusForm.UpdateAndShow(item);
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
        
        private void UpdateMessage(string status)
        {
            _liveClient.AsyncOperation.Post((o) =>
            {
                _statusForm.statusLabel.Text = status;
            }, null);
        }
    }
}

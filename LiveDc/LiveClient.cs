using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Forms;
using LiveDc.Properties;
using LiveDc.Utilites;
using SharpDc;
using SharpDc.Logging;
using SharpDc.Structs;
using Win32;
using DataReceivedEventArgs = Win32.DataReceivedEventArgs;

namespace LiveDc
{
    public partial class LiveClient
    {
        private readonly DcEngine _engine;
        private NotifyIcon _icon;
        private LiveDcDrive _drive;
        private CopyData _copyData;
        private DownloadItem _currentDownload;
        private FrmStatus _statusForm;
        private AsyncOperation _ao;

        public Settings Settings { get; private set; }

        public LiveClient()
        {
            if (!WindowsHelper.IsMagnetHandlerAssigned)
            {
                if (MessageBox.Show("Хотите чтобы LiveDC обрабатывал магнет-ссылки ?", "Вопрос", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    VistaSecurity.StartElevated("-reg");
                    return;
                }
            }

            InitializeComponent();

            Settings = new Settings();
            if (File.Exists(Settings.SettingsFilePath))
                Settings.Load();

            var r = new Random();

            LogManager.LogManagerInstance = new TraceLogManager();

            _engine = new DcEngine();
            //_engine.Settings.ActiveMode = false;
            _engine.TagInfo.Version = "livedc";

            _drive = new LiveDcDrive(_engine);
            _drive.MountAsync();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            
            var hub = _engine.Hubs.Add("hub2.o-go.ru", "livedc" + r.Next(0, 10000000));

            var settings = hub.Settings;

            settings.GetUsersList = true;
            //settings.PassiveMode = true;

            hub.Settings = settings;

            _engine.ActiveStatusChanged += EngineActivated;

            _engine.StartAsync();
            _engine.Connect();

            _copyData = new CopyData();
            _copyData.CreateHandle(new CreateParams());
            _copyData.Channels.Add("LIVEDC");
            _copyData.DataReceived += CopyDataDataReceived;

            _ao = AsyncOperationManager.CreateOperation(null);
            _statusForm = new FrmStatus();

        }

        void SettingsClick(object sender, EventArgs e)
        {

        }

        void CopyDataDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (_currentDownload != null)
                    _engine.RemoveDownload(_currentDownload);

                var magnet = Magnet.Parse((string)e.Data);
                _currentDownload = _engine.DownloadFile(magnet);

                new ThreadStart(FormThread).BeginInvoke(null, null);
            }
            catch (Exception x)
            {
                MessageBox.Show("Не удается начать загрузку: " + x.Message);
            }
        }

        private void FormThread()
        {
            _ao.Post((o) => _statusForm.Show(), null);

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.Seconds < 10)
            {
                if (_currentDownload.DoneSegmentsCount == 0)
                {
                    UpdateMessage(_currentDownload.Magnet.FileName, string.Format("{0}. Поиск. Найдено {1} источников ",
                                                Utils.FormatBytes(_currentDownload.Magnet.Size), _currentDownload.Sources.Count));
                }
                else
                {
                    UpdateMessage(_currentDownload.Magnet.FileName, string.Format("{0}. Открываю файл... ",
                                                Utils.FormatBytes(_currentDownload.Magnet.Size)));
                    Process.Start(Path.Combine(_drive.DriveRoot, _currentDownload.Magnet.FileName));
                    break;
                }

                if (_statusForm.DialogResult == DialogResult.Cancel)
                    break;

                Thread.Sleep(100);
            }

            if (_currentDownload.DoneSegmentsCount == 0)
            {
                UpdateMessage(_currentDownload.Magnet.FileName, string.Format("{0}. Не удается начать загрузку. Источников {1}.",
                            Utils.FormatBytes(_currentDownload.Magnet.Size), _currentDownload.Sources.Count));
            }

            Thread.Sleep(3000);

            if (!_statusForm.Visible)
            {
                _engine.RemoveDownload(_currentDownload);
                _currentDownload = null;
            }


            _ao.Post((o) => _statusForm.Hide(), null);
        }

        private void UpdateMessage(string message, string status)
        {
            _ao.Post((o) =>
                         {
                             _statusForm.MainText = message;
                             _statusForm.StatusText = status;
                         }, null);
        }

        void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // it is critically important to release the virtual drive
            _drive.Unmount();
        }

        void ProgramExitClick(object sender, EventArgs e)
        {
            _drive.Unmount();
            _engine.Dispose();
            _icon.Visible = false;
            Application.Exit();
        }

        void EngineActivated(object sender, EventArgs e)
        {
            if (_engine.Active)
            {
                System.Media.SystemSounds.Asterisk.Play();
                _icon.Text = "Статус: в сети";
                _icon.Icon = Resources.green;
            }
            else
            {
                _icon.Icon = Resources.AppIcon;
                _icon.Text = "Статус: отключен";
            }
        }
    }
}

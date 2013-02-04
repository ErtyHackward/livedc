using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Properties;
using LiveDc.Utilites;
using SharpDc;
using SharpDc.Logging;
using SharpDc.Structs;
using Win32;
using DataReceivedEventArgs = Win32.DataReceivedEventArgs;

namespace LiveDc
{
    public class LiveClient
    {
        private readonly DcEngine _engine;
        private NotifyIcon _icon;
        private LiveDcDrive _drive;
        private CopyData _copyData;
        private DownloadItem _currentDownload;
        private FrmStatus _statusForm;
        private AsyncOperation _ao;


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

            _icon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                Visible = true,
                Text = "Статус: отключен"
            };

            var appMenu = new ContextMenuStrip();
            appMenu.Items.Add("Выход").Click += ProgramExitClick;
            _icon.ContextMenuStrip = appMenu;

            var r = new Random();

            LogManager.LogManagerInstance = new TraceLogManager();

            _engine = new DcEngine();
            //_engine.Settings.ActiveMode = false;
            _engine.TagInfo.Version = "livedc";

            _drive = new LiveDcDrive(_engine);
            _drive.MountAsync();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
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
            _copyData.DataReceived += _copyData_DataReceived;

            _ao = AsyncOperationManager.CreateOperation(null);
            _statusForm = new FrmStatus();

        }

        void _copyData_DataReceived(object sender, DataReceivedEventArgs e)
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
                    UpdateMessage(string.Format("{0} {1}\nПоиск. Найдено {2} источников ",
                                                _currentDownload.Magnet.FileName,
                                                Utils.FormatBytes(_currentDownload.Magnet.Size), _currentDownload.Sources.Count));
                }
                else
                {
                    UpdateMessage(string.Format("{0} {1}\nОткрываю файл... ",
                                                _currentDownload.Magnet.FileName,
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
                UpdateMessage(string.Format("{0} {1}\nНе удается начать загрузку. Источников {2}.",
                            _currentDownload.Magnet.FileName,
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

        private void UpdateMessage(string message)
        {
            _ao.Post((o) => { _statusForm.MainText = message; }, null);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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

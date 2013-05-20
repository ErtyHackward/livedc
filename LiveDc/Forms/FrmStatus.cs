using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using LiveDc.Windows;
using SharpDc;

namespace LiveDc.Forms
{
    public partial class FrmStatus : Form
    {
        private readonly LaunchManager _launchManager;

        public FrmStatus(LaunchManager launchManager)
        {
            if (launchManager == null) 
                throw new ArgumentNullException("launchManager");

            _launchManager = launchManager;
            InitializeComponent();
            Closing += FrmStatus_Closing;
            NativeImageList.LargeExtensionImageLoaded += NativeImageList_LargeExtensionImageLoaded;
        }

        void NativeImageList_LargeExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            if (_launchManager.Magnet.FileName != null && e.Extension.ToLower() == Path.GetExtension(_launchManager.Magnet.FileName).ToLower())
            {
                _launchManager.Client.AsyncOperation.Post(o => iconPicture.Image = e.Icon, null);
            }
        }

        void FrmStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            _launchManager.Cancel();
            Hide();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _launchManager.Cancel();
            Hide();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            _launchManager.OpenFile();
        }

        private void queueButton_Click(object sender, EventArgs e)
        {
            _launchManager.AddToQueue();
            Hide();
        }

        public void UpdateAndShow()
        {
            iconPicture.Image = NativeImageList.TryGetLargeIcon(Path.GetExtension(_launchManager.Magnet.FileName));

            nameLabel.Text = _launchManager.Magnet.FileName;
            sizeLabel.Text = Utils.FormatBytes(_launchManager.Magnet.Size);

            statusLabel.Text = "";
            startButton.Enabled = false;
            Show();
        }

        public void UpdateStartButton(int timeout = -1)
        {
            string label = "";

            switch (Path.GetExtension(_launchManager.Magnet.FileName).ToLower())
            {
                case ".avi":
                case ".mov":
                case ".mkv":
                case ".3gp":
                case ".wmv":
                case ".mpg":
                case ".ts":
                    label = "Начать просмотр";
                    break;
                default:
                    label = "Открыть";
                    break;
            }

            startButton.Enabled = true;
            if (timeout >= 0)
                startButton.Text = string.Format("{0} ({1})", label, timeout);
            else
                startButton.Text = label;
        }
    }
}

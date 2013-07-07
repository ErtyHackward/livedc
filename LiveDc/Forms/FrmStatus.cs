using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using LiveDc.Windows;
using SharpDc;

namespace LiveDc.Forms
{
    public partial class FrmStatus : Form
    {
        private readonly AsyncOperation _ao;
        private IStartItem _startItem;
        
        public FrmStatus(AsyncOperation ao)
        {
            _ao = ao;

            InitializeComponent();
            Closing += FrmStatus_Closing;
            NativeImageList.LargeExtensionImageLoaded += NativeImageList_LargeExtensionImageLoaded;
        }

        void NativeImageList_LargeExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            if (_startItem.Magnet.FileName != null && e.Extension.ToLower() == Path.GetExtension(_startItem.Magnet.FileName).ToLower())
            {
                _ao.Post(o => iconPicture.Image = e.Icon, null);
            }
        }

        void FrmStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            _startItem.Dispose();
            Hide();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _startItem.Dispose();
            Hide();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            _startItem.OpenFile();
            Hide();
        }

        private void queueButton_Click(object sender, EventArgs e)
        {
            _startItem.AddToQueue();
            Hide();
        }

        public void UpdateAndShow(IStartItem startItem)
        {
            _startItem = startItem;
            iconPicture.Image = NativeImageList.TryGetLargeIcon(Path.GetExtension(startItem.Magnet.FileName));

            nameLabel.Text = startItem.Magnet.FileName;
            sizeLabel.Text = Utils.FormatBytes(startItem.Magnet.Size);

            progressBar.Enabled = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            statusLabel.Text = "";
            UpdateStartButton();
            startButton.Enabled = false;
            Show();
        }

        public void UpdateStartButton(int timeout = -1)
        {
            string label = "";

            switch (Path.GetExtension(_startItem.Magnet.FileName).ToLower())
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

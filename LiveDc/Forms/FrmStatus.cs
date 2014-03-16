using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using LiveDc.Providers;
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
            FormClosing += OnFormClosing;
            NativeImageList.LargeExtensionImageLoaded += NativeImageList_LargeExtensionImageLoaded;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _startItem.Dispose();
                Hide();
            }
        }

        void NativeImageList_LargeExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            if (_startItem != null && _startItem.Magnet.FileName != null && e.Extension.ToLower() == Path.GetExtension(_startItem.Magnet.FileName).ToLower())
            {
                _ao.Post(o => iconPicture.Image = e.Icon, null);
            }
        }

        void FrmStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

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

            if (!string.IsNullOrEmpty(startItem.Magnet.FileName))
            {
                iconPicture.Image = NativeImageList.TryGetLargeIcon(Path.GetExtension(startItem.Magnet.FileName));
                nameLabel.Text = startItem.Magnet.FileName;

                if (startItem.Magnet.Size != 0)
                {
                    sizeLabel.Text = Utils.FormatBytes(startItem.Magnet.Size);
                }
                else
                {
                    sizeLabel.Text = null;
                }
            }
            else
            {
                iconPicture.Image = null;
                nameLabel.Text = null;
                sizeLabel.Text = null;
            }

            progressBar.Enabled = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            statusLabel.Text = "";
            UpdateStartButton();
            startButton.Enabled = false;
            Show();
            Activate();
        }

        public void UpdateStartButton(int timeout = -1)
        {
            string label;

            if (!string.IsNullOrEmpty(_startItem.Magnet.FileName))
            {
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
            }
            else
            {
                label = "Открыть";
            }

            startButton.Enabled = true;
            if (timeout >= 0)
                startButton.Text = string.Format("{0} ({1})", label, timeout);
            else
                startButton.Text = label;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_startItem == null)
                return;
            
            startButton.Enabled = _startItem.ReadyToStart;
            statusLabel.Text = _startItem.StatusMessage;

            if (float.IsNaN(_startItem.Progress))
            {
                progressBar.Enabled = false;
            }
            else if (float.IsPositiveInfinity(_startItem.Progress))
            {
                progressBar.Enabled = true;
                progressBar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                progressBar.Enabled = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = (int)(100 * _startItem.Progress);
            }

            if (nameLabel.Text != _startItem.Magnet.FileName && !string.IsNullOrEmpty(_startItem.Magnet.FileName))
            {
                iconPicture.Image = NativeImageList.TryGetLargeIcon(Path.GetExtension(_startItem.Magnet.FileName));
                nameLabel.Text = _startItem.Magnet.FileName;
            }

            if (string.IsNullOrEmpty(sizeLabel.Text) && _startItem.Magnet.Size != 0)
            {
                sizeLabel.Text = Utils.FormatBytes(_startItem.Magnet.Size);
            }

            if (!_startItem.Closed)
                _startItem.MainThreadAction(this);

            if (_startItem.Closed)
            {
                Close();
                _startItem = null;
            }
        }
    }
}

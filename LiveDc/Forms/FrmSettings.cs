using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Utilites;

namespace LiveDc.Forms
{
    public partial class FrmSettings : Form
    {
        private readonly LiveClient _client;

        public FrmSettings(LiveClient client)
        {
            _client = client;
            InitializeComponent();
        }

        private void FrmSettings_Load(object sender, System.EventArgs e)
        {
            autostartCheck.Checked = WindowsHelper.IsInAutoRun;
            storagePathText.Text = _client.Settings.StorageAutoSelect ? StorageHelper.GetBestSaveDirectory() : _client.Settings.StoragePath;
            storageAutoselectCheck.Checked = _client.Settings.StorageAutoSelect;
        }

        private void storageSelectButton_Click(object sender, System.EventArgs e)
        {
            var dialog = new FolderBrowserDialog();

            dialog.Description = "Выберите папку куда нужно скачивать файлы";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                storagePathText.Text = dialog.SelectedPath;
            }
        }

        private void storageAutoselectCheck_Click(object sender, System.EventArgs e)
        {
            if (storageAutoselectCheck.Checked)
            {
                storageSelectButton.Enabled = false;
                storagePathText.Enabled = false;
                storagePathText.Text = StorageHelper.GetBestSaveDirectory();
            }
            else
            {
                storageSelectButton.Enabled = true;
                storagePathText.Enabled = true;
            }
        }

        private void applyButton_Click(object sender, System.EventArgs e)
        {
            if (WindowsHelper.IsInAutoRun != autostartCheck.Checked)
            {
                VistaSecurity.StartElevated(autostartCheck.Checked ? "-setstartup" : "-removestartup");
            }

            _client.Settings.StorageAutoSelect = storageAutoselectCheck.Checked;
            if (!_client.Settings.StorageAutoSelect)
            {
                _client.Settings.StoragePath = storagePathText.Text;
            }

            Close();
        }

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }


    }
}

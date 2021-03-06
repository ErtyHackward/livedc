﻿using System.Linq;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Providers;
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
            storageAutopruneCheck.Checked = _client.Settings.StorageAutoPrune;

            tcpPortNumeric.Value = _client.Settings.TCPPort;
            udpPortNumeric.Value = _client.Settings.UDPPort;

            startPageCheck.Checked = _client.Settings.OpenStartPage;
            startPageUrlText.Text = _client.Settings.StartPageUrl;

            torrentAssocCheck.Checked = _client.Settings.AssocTorrentFiles;
            magnetAssocCheck.Checked = _client.Settings.AssocMagnetLinks;

            storageAutoselectCheck_Click(null, null);
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

            _client.Settings.OpenStartPage = startPageCheck.Checked;
            _client.Settings.StartPageUrl = startPageUrlText.Text;
            _client.Settings.StorageAutoSelect = storageAutoselectCheck.Checked;
            _client.Settings.StorageAutoPrune = storageAutopruneCheck.Checked;

            _client.Settings.AssocTorrentFiles = torrentAssocCheck.Checked;
            _client.Settings.AssocMagnetLinks = magnetAssocCheck.Checked;

            if (!_client.Settings.StorageAutoSelect)
            {
                _client.Settings.StoragePath = storagePathText.Text;
            }

            if (tcpPortNumeric.Value != 0)
            {
                _client.Settings.TCPPort = (int)tcpPortNumeric.Value;
                _client.Providers.OfType<DcProvider>().First().Engine.Settings.TcpPort = _client.Settings.TCPPort;
            }

            if (udpPortNumeric.Value != 0)
            {
                _client.Settings.UDPPort = (int)udpPortNumeric.Value;
                _client.Providers.OfType<DcProvider>().First().Engine.Settings.UdpPort = _client.Settings.UDPPort;
            }
            
            

            _client.Settings.Save();

            Close();
        }

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }


    }
}

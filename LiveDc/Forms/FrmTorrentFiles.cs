using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiveDc.Windows;
using MonoTorrent.Common;
using SharpDc;

namespace LiveDc.Forms
{
    public partial class FrmTorrentFiles : Form
    {
        private AsyncOperation _ao;
        private readonly Torrent _torrent;

        public TorrentFile SelectedFile { get; private set; }

        public FrmTorrentFiles(Torrent torrent)
        {
            _torrent = torrent;
            InitializeComponent();

            _ao = AsyncOperationManager.CreateOperation(null);

            NativeImageList.SetListViewIconIndex(listView1.Handle);
            NativeImageList.SmallExtensionImageLoaded += NativeImageList_SmallExtensionImageLoaded;

            foreach (var torrentFile in _torrent.Files)
            {
                var lvi = new ListViewItem();

                lvi.Text = torrentFile.Path;
                lvi.SubItems.Add(Utils.FormatBytes(torrentFile.Length));
                lvi.Tag = torrentFile;
                lvi.ImageIndex = NativeImageList.TryFileIconIndex(torrentFile.Path);
                
                listView1.Items.Add(lvi);
            }

            listView1.Sort();
        }

        private void UpdateIcon(string ext, int index)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if (Path.GetExtension(item.Text) == ext)
                {
                    item.ImageIndex = index;
                }
            }
        }

        void NativeImageList_SmallExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            _ao.Post(delegate { UpdateIcon(e.Extension, e.Index); }, null);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                openButton.Enabled = true;
                SelectedFile = (TorrentFile)listView1.SelectedItems[0].Tag;
            }
            else
            {
                openButton.Enabled = false;
                SelectedFile = null;
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
                openButton_Click(null, null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Managers;
using LiveDc.Providers;
using LiveDc.Windows;
using SharpDc;
using SharpDc.Managers;
using SharpDc.Messages;

namespace LiveDc.Forms
{
    public partial class FrmSearch : Form
    {
        private readonly LiveClient _client;
        private readonly DcProvider _dcProvider;

        private DateTime _lastUpdate;

        private Dictionary<string, HubSearchResult> _results = new Dictionary<string, HubSearchResult>();

        private List<HubSearchResult> _list = new List<HubSearchResult>();

        private IComparer<HubSearchResult> _comparer;

        public FrmSearch(LiveClient client, DcProvider dcProvider)
        {
            _client = client;
            _dcProvider = dcProvider;
            InitializeComponent();

            _comparer = new SourceComparer();
            
            NativeImageList.SetListViewIconIndex(listView1.Handle);
            NativeImageList.SmallExtensionImageLoaded += NativeImageListSmallExtensionImageLoaded;

            _dcProvider.Engine.SearchManager.SearchStarted += SearchManager_SearchStarted;
            _dcProvider.Engine.SearchManager.SearchResult += SearchManager_SearchResult;
        }

        void NativeImageListSmallExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            listView1.Invoke(new Action(RefreshList));
        }

        void RefreshList()
        {
            if (Monitor.TryEnter(listView1))
            {
                listView1.Refresh();
                Monitor.Exit(listView1);
            }
        }

        void SearchManager_SearchResult(object sender, SearchManagerResultEventArgs e)
        {
            // skip folders
            if (e.Result.Size == -1)
                return;

            lock (_results)
            {
                if (!_results.ContainsKey(e.Result.Magnet.TTH))
                {
                    _results.Add(e.Result.Magnet.TTH, e.Result);
                    var pos = _list.BinarySearch(e.Result, _comparer);

                    if (pos < 0)
                    {
                        _list.Insert(~pos, e.Result);
                    }
                    else
                    {
                        _list.Insert(pos, e.Result);
                    }
                }
                else
                {
                    // change src count
                    _list.Sort(_comparer);
                }
            }

            if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 200)
            {
                _lastUpdate = DateTime.Now;
                _client.AsyncOperation.Post(o => FillList(), null);
            }
        }

        private void FillList()
        {
            using (new PerfLimit("ListRefresh"))
            {
                listView1.BeginUpdate();
                listView1.VirtualListSize = _list.Count;
                listView1.Update();
                listView1.EndUpdate();
            }
        }

        void SearchManager_SearchStarted(object sender, SharpDc.Managers.SearchEventArgs e)
        {
            if (e.Message.SearchType == SearchType.TTH)
                return;

            lock (_results)
            {
                _results.Clear();
                _list.Clear();
            }

            _client.AsyncOperation.Post(o => { listView1.VirtualListSize = 0; listView1.Refresh(); }, null);
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            _dcProvider.Engine.SearchManager.Search(new SearchMessage { SearchRequest = textBox1.Text, SearchType = SearchType.Any });
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            HubSearchResult hsr;
            lock (_results)
            {
                hsr = _list[e.ItemIndex];
            }

            var item = new ListViewItem(hsr.Magnet.FileName);
            item.SubItems.Add(hsr.Sources.Count.ToString());
            item.SubItems.Add(Utils.FormatBytes(hsr.Magnet.Size));
            item.Tag = hsr;
            item.ImageIndex = NativeImageList.TryFileIconIndex(Path.GetExtension(hsr.Magnet.FileName));

            e.Item = item;
            
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                var index = listView1.SelectedIndices[0];
                var hsr = _list[index];

                _client.StartFile(hsr.Magnet);
            }
        }
    }

    public class ListViewNoFlicker : ListView
    {
        public ListViewNoFlicker()
        {
            //Activate double buffering
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            //Enable the OnNotifyMessage event so we get a chance to filter out 
            // Windows messages before they get to the form's WndProc
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
        }

        protected override void OnNotifyMessage(Message m)
        {
            //Filter out the WM_ERASEBKGND message
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }
    }

    public class SourceComparer : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return y.Sources.Count.CompareTo(x.Sources.Count);
        }
    }

    public class SizeComparer : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return y.Size.CompareTo(x.Size);
        }
    }
}


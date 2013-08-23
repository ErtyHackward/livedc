using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
            resultsListView.SetSortIcon(1, SortOrder.Descending);
            

            NativeImageList.SetListViewIconIndex(resultsListView.Handle);
            NativeImageList.SmallExtensionImageLoaded += NativeImageListSmallExtensionImageLoaded;

            _dcProvider.Engine.SearchManager.SearchStarted += SearchManager_SearchStarted;
            _dcProvider.Engine.SearchManager.SearchResult += SearchManager_SearchResult;
        }

        void NativeImageListSmallExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            resultsListView.Invoke(new Action(RefreshList));
        }

        void RefreshList()
        {
            if (Monitor.TryEnter(resultsListView))
            {
                resultsListView.Refresh();
                Monitor.Exit(resultsListView);
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
                resultsListView.BeginUpdate();
                resultsListView.VirtualListSize = _list.Count;
                resultsListView.Update();
                resultsListView.EndUpdate();
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

            _client.AsyncOperation.Post(o => { resultsListView.VirtualListSize = 0; resultsListView.Refresh(); }, null);
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            for (var i = 0; i < resultsListView.Columns.Count; i++)
            {
                resultsListView.SetSortIcon(i, SortOrder.None);
            }

            var column = resultsListView.Columns[e.Column];
            
            if (column == fileNameColumn)
            {
                if (_comparer is NameComparer)
                {
                    _comparer = new NameComparerAsc();
                    resultsListView.SetSortIcon(e.Column, SortOrder.Ascending);
                }
                else
                {
                    _comparer = new NameComparer();
                    resultsListView.SetSortIcon(e.Column, SortOrder.Descending);
                }
                
            }
            else if (column == sourcesColumn)
            {
                if (_comparer is SourceComparer)
                {
                    _comparer = new SourceComparerAsc();
                    resultsListView.SetSortIcon(e.Column, SortOrder.Ascending);
                }
                else
                {
                    _comparer = new SourceComparer();
                    resultsListView.SetSortIcon(e.Column, SortOrder.Descending);
                }
            }
            else if (column == sizeColumn)
            {
                if (_comparer is SizeComparer)
                {
                    _comparer = new SizeComparerAsc();
                    resultsListView.SetSortIcon(e.Column, SortOrder.Ascending);
                }
                else
                {
                    _comparer = new SizeComparer();
                    resultsListView.SetSortIcon(e.Column, SortOrder.Descending);
                }
            }

            _list.Sort(_comparer);
            FillList();
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
            if (resultsListView.SelectedIndices.Count > 0)
            {
                var index = resultsListView.SelectedIndices[0];
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

    public class SourceComparerAsc : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return x.Sources.Count.CompareTo(y.Sources.Count);
        }
    }

    public class SizeComparer : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return y.Size.CompareTo(x.Size);
        }
    }

    public class SizeComparerAsc : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return x.Size.CompareTo(y.Size);
        }
    }

    public class NameComparer : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return y.Magnet.FileName.CompareTo(x.Magnet.FileName);
        }
    }

    public class NameComparerAsc : IComparer<HubSearchResult>
    {
        public int Compare(HubSearchResult x, HubSearchResult y)
        {
            return x.Magnet.FileName.CompareTo(y.Magnet.FileName);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ListViewExtensions
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct HDITEM
        {
            public Mask mask;
            public int cxy;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public Format fmt;
            public IntPtr lParam;
            // _WIN32_IE >= 0x0300 
            public int iImage;
            public int iOrder;
            // _WIN32_IE >= 0x0500
            public uint type;
            public IntPtr pvFilter;
            // _WIN32_WINNT >= 0x0600
            public uint state;

            [Flags]
            public enum Mask
            {
                Format = 0x4,       // HDI_FORMAT
            };

            [Flags]
            public enum Format
            {
                SortDown = 0x200,   // HDF_SORTDOWN
                SortUp = 0x400,     // HDF_SORTUP
            };
        };

        public const int LVM_FIRST = 0x1000;
        public const int LVM_GETHEADER = LVM_FIRST + 31;

        public const int HDM_FIRST = 0x1200;
        public const int HDM_GETITEM = HDM_FIRST + 11;
        public const int HDM_SETITEM = HDM_FIRST + 12;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, ref HDITEM lParam);

        public static void SetSortIcon(this ListView listViewControl, int columnIndex, SortOrder order)
        {
            IntPtr columnHeader = SendMessage(listViewControl.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            for (int columnNumber = 0; columnNumber <= listViewControl.Columns.Count - 1; columnNumber++)
            {
                var columnPtr = new IntPtr(columnNumber);
                var item = new HDITEM
                {
                    mask = HDITEM.Mask.Format
                };

                if (SendMessage(columnHeader, HDM_GETITEM, columnPtr, ref item) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                if (order != SortOrder.None && columnNumber == columnIndex)
                {
                    switch (order)
                    {
                        case SortOrder.Ascending:
                            item.fmt &= ~HDITEM.Format.SortDown;
                            item.fmt |= HDITEM.Format.SortUp;
                            break;
                        case SortOrder.Descending:
                            item.fmt &= ~HDITEM.Format.SortUp;
                            item.fmt |= HDITEM.Format.SortDown;
                            break;
                    }
                }
                else
                {
                    item.fmt &= ~HDITEM.Format.SortDown & ~HDITEM.Format.SortUp;
                }

                if (SendMessage(columnHeader, HDM_SETITEM, columnPtr, ref item) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }
        }
    }
}


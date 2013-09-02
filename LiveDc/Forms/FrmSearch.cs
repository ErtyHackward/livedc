using System;
using System.Collections.Generic;
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

        private readonly Dictionary<string, HubSearchResult> _results = new Dictionary<string, HubSearchResult>();

        private List<HubSearchResult> _list = new List<HubSearchResult>();

        private IComparer<HubSearchResult> _comparer;
        private SearchMessage _searchMsg;

        public FrmSearch(LiveClient client, DcProvider dcProvider)
        {
            _client = client;
            _dcProvider = dcProvider;
            InitializeComponent();

            _comparer = new SourceComparer();

            SourcesColumn.HeaderCell.SortGlyphDirection = SortOrder.Descending;

            NativeImageList.SmallExtensionImageLoaded += NativeImageListLargeExtensionImageLoaded;
            
            _dcProvider.Engine.SearchManager.SearchStarted += SearchManagerSearchStarted;
            _dcProvider.Engine.SearchManager.SearchResult += SearchManagerSearchResult;
        }

        void NativeImageListLargeExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            resultsDataGridView.Invoke(new Action(RefreshList));
        }
        
        void RefreshList()
        {
            if (Monitor.TryEnter(resultsDataGridView))
            {
                resultsDataGridView.Refresh();
                Monitor.Exit(resultsDataGridView);
            }
        }

        void SearchManagerSearchResult(object sender, SearchManagerResultEventArgs e)
        {
            // skip folders
            if (e.Result.Size == -1)
                return;

            if (infoPanel.Visible)
                infoPanel.BeginInvoke((Action)(() => infoPanel.Hide()));

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
            if (resultsDataGridView.Rows.Count == _list.Count || resultsDataGridView.IsDisposed)
                return;

            using (new PerfLimit("ListRefresh"))
            {
                resultsDataGridView.SuspendDrawing();

                if (resultsDataGridView.Rows.Count > _list.Count)
                {
                    resultsDataGridView.Rows.Clear();
                }

                if (_list.Count > 0)
                {
                    var addRows = _list.Count - resultsDataGridView.Rows.Count;

                    if (resultsDataGridView.Rows.Count == 0)
                    {
                        resultsDataGridView.Rows.Add();
                        addRows--;
                    }

                    if (addRows > 1)
                        resultsDataGridView.Rows.AddCopies(0, addRows);
                }
                resultsDataGridView.ResumeDrawing(true);
            }
        }

        void SearchManagerSearchStarted(object sender, SearchEventArgs e)
        {
            if (e.Message.SearchType == SearchType.TTH)
                return;

            lock (_results)
            {
                _results.Clear();
                _list.Clear();
            }

            _client.AsyncOperation.Post(o => FillList(), null);

            infoPanel.BeginInvoke((Action)(() =>
            {
                infoPanel.Show();
                infoLabel.Text = "Идет поиск...";
            }));
            
        }

        private void Button1Click(object sender, EventArgs e)
        {
            _searchMsg = new SearchMessage { SearchRequest = textBox1.Text, SearchType = SearchType.Any };
            _dcProvider.Engine.SearchManager.Search(_searchMsg);
        }

        private void ResultsDataGridViewCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            HubSearchResult hsr;
            lock (_results)
            {
                hsr = _list[e.RowIndex];
            }

            if (e.ColumnIndex == IconColumn.Index)
            {
                e.Value = NativeImageList.TryGetSmallIcon(Path.GetExtension(hsr.Magnet.FileName));
            }
            else if (e.ColumnIndex == FileNameColumn.Index)
            {
                e.Value = hsr.Magnet.FileName;
            }
            else if (e.ColumnIndex == SourcesColumn.Index)
            {
                e.Value = hsr.Sources.Count;
            }
            else if (e.ColumnIndex == SizeColumn.Index)
            {
                e.Value = hsr.Magnet.Size;
            }
        }

        private void ResultsDataGridViewColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            foreach (DataGridViewColumn c in resultsDataGridView.Columns)
            {
                c.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            var column = resultsDataGridView.Columns[e.ColumnIndex];

            if (column == FileNameColumn)
            {
                if (_comparer is NameComparer)
                {
                    _comparer = new NameComparerAsc();
                    column.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                {
                    _comparer = new NameComparer();
                    column.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }

            }
            else if (column == SourcesColumn)
            {
                if (_comparer is SourceComparer)
                {
                    _comparer = new SourceComparerAsc();
                    column.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                {
                    _comparer = new SourceComparer();
                    column.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            }
            else if (column == SizeColumn)
            {
                if (_comparer is SizeComparer)
                {
                    _comparer = new SizeComparerAsc();
                    column.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                {
                    _comparer = new SizeComparer();
                    column.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            }

            _list.Sort(_comparer);
            FillList();
        }

        private void Timer1Tick(object sender, EventArgs e)
        {
            var estimate = _dcProvider.Engine.SearchManager.EstimateSearch(_searchMsg);

            if (estimate != TimeSpan.MaxValue)
            {
                infoLabel.Text = string.Format("Поиск начнется через {0} с", Math.Floor(estimate.TotalSeconds));
                infoPanel.Show();
            }
        }

        private void ResultsDataGridViewCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == SizeColumn.Index)
            {
                e.FormattingApplied = true;
                e.Value = Utils.FormatBytes((long)e.Value);
            }
        }

        private void resultsDataGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (resultsDataGridView.SelectedRows.Count > 0)
            {
                var row = resultsDataGridView.SelectedRows[0];
                var hsr = _list[row.Index];

                _client.StartFile(hsr.Magnet);
            }
        }

        private void infoPanel_SizeChanged(object sender, EventArgs e)
        {
            infoPanel.Left = (resultsDataGridView.Width - infoPanel.Width) / 2;
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

    public static class ControlHelper
    {
        #region Redraw Suspend/Resume
        [DllImport("user32.dll", EntryPoint = "SendMessageA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private const int WM_SETREDRAW = 0xB;

        public static void SuspendDrawing(this Control target)
        {
            SendMessage(target.Handle, WM_SETREDRAW, 0, 0);
        }

        public static void ResumeDrawing(this Control target) { ResumeDrawing(target, true); }
        public static void ResumeDrawing(this Control target, bool redraw)
        {
            SendMessage(target.Handle, WM_SETREDRAW, 1, 0);

            if (redraw)
            {
                target.Refresh();
            }
        }
        #endregion
    }
}


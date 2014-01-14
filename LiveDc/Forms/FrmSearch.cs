using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Providers;
using LiveDc.Windows;
using SharpDc;
using SharpDc.Interfaces;
using SharpDc.Managers;
using SharpDc.Messages;

namespace LiveDc.Forms
{
    public partial class FrmSearch : Form
    {
        private static List<IWebSearchProvider> _providers;

        public static bool SearchProvidersLoaded { get { return _providers != null; } }

        public static void LoadSearchProviders(string pluginFolder)
        {
            _providers = new List<IWebSearchProvider>();

            var iface = typeof(IWebSearchProvider);

            if (!Directory.Exists(pluginFolder))
                return;
            
            foreach (var libPath in Directory.GetFiles(pluginFolder, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFile(libPath);
                    
                    foreach (var exportedType in assembly.GetExportedTypes().Where(iface.IsAssignableFrom))
                    {
                        var provider =(IWebSearchProvider)Activator.CreateInstance(exportedType);
                        _providers.Add(provider);
                        Logger.Info("{0} provider is loaded", provider.ProviderName);
                    }
                }
                catch (Exception x)
                {
                    Logger.Error("Unable to load library: {0}/r/n{1}", libPath, x.Message);
                }
            }
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LiveClient _client;
        private readonly DcProvider _dcProvider;

        private DateTime _lastUpdate;

        private readonly Dictionary<string, ISearchResult> _results = new Dictionary<string, ISearchResult>();

        private readonly List<ISearchResult> _list = new List<ISearchResult>();

        private IComparer<ISearchResult> _comparer;
        private SearchMessage _searchMsg;
        

        public FrmSearch(LiveClient client, DcProvider dcProvider)
        {
            _client = client;
            _dcProvider = dcProvider;
            InitializeComponent();

            _comparer = new SourceComparer();

            SourcesColumn.HeaderCell.SortGlyphDirection = SortOrder.Descending;

            NativeImageList.LargeExtensionImageLoaded += NativeImageListLargeExtensionImageLoaded;
            
            _dcProvider.Engine.SearchManager.SearchStarted += SearchManagerSearchStarted;
            _dcProvider.Engine.SearchManager.SearchResult += SearchManagerSearchResult;

            if (!SearchProvidersLoaded)
                LoadSearchProviders(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),"Plugins"));

            IntPtr h = tabControl1.Handle;
            foreach (var webSearchProvider in _providers)
            {
                var tab = new TabPage(webSearchProvider.TabTitle);
                var flowPanel = new FlowLayoutPanel();
                tab.Controls.Add(flowPanel);
                flowPanel.Dock = DockStyle.Fill;
                flowPanel.Padding = new Padding(0, 0, SystemInformation.VerticalScrollBarWidth, 0);
                flowPanel.BackColor = SystemColors.Window;
                flowPanel.AutoScroll = true;
                tab.Tag = webSearchProvider;
                tabControl1.TabPages.Insert(0, tab);
            }
            tabControl1.SelectTab(0);

            Disposed += FrmSearch_Disposed;
        }

        void FrmSearch_Disposed(object sender, EventArgs e)
        {
            NativeImageList.LargeExtensionImageLoaded -= NativeImageListLargeExtensionImageLoaded;

            _dcProvider.Engine.SearchManager.SearchStarted -= SearchManagerSearchStarted;
            _dcProvider.Engine.SearchManager.SearchResult -= SearchManagerSearchResult;
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

        private void HandleResult(ISearchResult result)
        {
            // skip folders
            if (result.Size == -1)
                return;

            if (infoPanel.Visible)
                infoPanel.BeginInvoke((Action)(() => infoPanel.Hide()));

            lock (_results)
            {
                var hsr = result as HubSearchResult;

                if (hsr != null)
                {
                    if (!_results.ContainsKey(hsr.Magnet.TTH))
                    {
                        _results.Add(hsr.Magnet.TTH, hsr);
                        InsertResult(hsr);
                    }
                    else
                    {
                        // change src count
                        _list.Sort(_comparer);
                    }
                }
                else
                {
                    // provider result
                    InsertResult(result);
                }
            }

            if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 200)
            {
                _lastUpdate = DateTime.Now;
                _client.AsyncOperation.Post(o => FillList(), null);
            }
        }

        void DcSharaResPosterReceived(object sender, EventArgs e)
        {
            var dcSharaRes = (WebSearchResult)sender;
            dcSharaRes.PosterReceived -= DcSharaResPosterReceived;
            _client.AsyncOperation.Post(o => 
            {
                foreach (TabPage tabPage in tabControl1.TabPages)
                {
                    if (tabPage == hubsTabPage)
                        continue;

                    var flowPanel = (FlowLayoutPanel)tabPage.Controls[0];
                    foreach (PosterControl control in flowPanel.Controls)
                    {
                        if (control.Tag == dcSharaRes)
                        {
                            control.Poster = dcSharaRes.Poster;
                            return;
                        }
                    }
                }
            }, null);
        }

        private void InsertResult(ISearchResult result)
        {
            var pos = _list.BinarySearch(result, _comparer);

            if (pos < 0)
            {
                _list.Insert(~pos, result);
            }
            else
            {
                _list.Insert(pos, result);
            }
        }

        void SearchManagerSearchResult(object sender, SearchManagerResultEventArgs e)
        {
            HandleResult(e.Result);
        }

        private void FillList()
        {
            if (resultsDataGridView.Rows.Count == _list.Count || resultsDataGridView.IsDisposed)
            {
                resultsDataGridView.Refresh();
                return;
            }

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
                        resultsDataGridView.Rows[0].Height = 32;
                        addRows--;
                    }

                    if (addRows >= 1)
                        resultsDataGridView.Rows.AddCopies(0, addRows);
                }
                resultsDataGridView.ResumeDrawing(true);
            }

            hubsTabPage.Text = string.Format("На хабах ({0})", _list.Count);
        }

        private TabPage FindByProvider(IWebSearchProvider provider)
        {
            return tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(tabPage => tabPage.Tag == provider);
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

            ProvidersSearch();

            _client.AsyncOperation.Post(o => FillList(), null);

            infoPanel.BeginInvoke((Action)(() =>
            {
                infoPanel.Show();
                infoLabel.Text = "Идет поиск...";
            }));
            
        }

        private void Button1Click(object sender, EventArgs e)
        {
            resultsDataGridView.Rows.Clear();
            _searchMsg = new SearchMessage { SearchRequest = textBox1.Text, SearchType = SearchType.Any };
            _dcProvider.Engine.SearchManager.Search(_searchMsg);

        }

        private bool ProvidersSearch()
        {
            foreach (var searchProvider in _providers)
            {
                searchProvider.SearchAsync(textBox1.Text, ProvidersResults);
            }

            return true;
        }

        private void ProvidersResults(WebSearchResponse response)
        {
            if (response.Results == null)
            {
                _client.AsyncOperation.Post(o =>
                    {
                        var tab = FindByProvider(response.Provider);
                        tab.Text = string.Format("{0} (0)", response.Provider.TabTitle);
                    }, null);
                return;
            }

            foreach (var result in response.Results)
            {
                result.PosterReceived += DcSharaResPosterReceived;                
            }

            _client.AsyncOperation.Post(o =>
            {
                infoPanel.Hide();

                var tab = FindByProvider(response.Provider);

                tab.Text = string.Format("{0} ({1})", response.Provider.TabTitle, response.Results.Count);

                var flowPanel = (FlowLayoutPanel)tab.Controls[0];

                flowPanel.Controls.Clear();

                flowPanel.SuspendDrawing();
                foreach (var providerResult in response.Results)
                {
                    var poster = providerResult.Poster;
                    
                    var control = new PosterControl { 
                        Text = providerResult.Name, 
                        Tag = providerResult
                    };

                    if (poster != null)
                        control.Poster = poster;

                    control.Click += control_Click;

                    flowPanel.Controls.Add(control);
                }
                flowPanel.ResumeDrawing(true);
                flowPanel.Refresh();
            }, null);
        }

        void control_Click(object sender, EventArgs e)
        {
            var control = (PosterControl)sender;
            var res = (WebSearchResult)control.Tag;
            ShellHelper.Start(res.ReleaseUrl);
        }

        private void ResultsDataGridViewCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            ISearchResult result;
            lock (_results)
            {
                result = _list[e.RowIndex];
            }

            var hsr = result as HubSearchResult;
            var dcsResult = result as WebSearchResult;

            if (e.ColumnIndex == IconColumn.Index)
            {
                if (dcsResult != null)
                {
                    e.Value = dcsResult.Poster;
                }
                else
                {
                    e.Value = NativeImageList.TryGetLargeIcon(Path.GetExtension(result.Name));
                }
            }
            else if (e.ColumnIndex == FileNameColumn.Index)
            {
                e.Value = result.Name;
            }
            else if (e.ColumnIndex == SourcesColumn.Index)
            {
                if (hsr != null)
                {
                    e.Value = hsr.Sources.Count;
                }
                else
                {
                    e.Value = 0;
                }
            }
            else if (e.ColumnIndex == SizeColumn.Index)
            {
                e.Value = result.Size;
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
                e.Value = (long)e.Value == 0 ? "" : Utils.FormatBytes((long)e.Value);
            }
        }

        private void resultsDataGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (resultsDataGridView.SelectedRows.Count > 0)
            {
                var row = resultsDataGridView.SelectedRows[0];
                var res = _list[row.Index];

                if (res is HubSearchResult)
                {
                    var hsr = (HubSearchResult)res;
                    _client.StartFile(hsr.Magnet);
                }

                if (res is WebSearchResult)
                {
                    var dsres = (WebSearchResult)res;
                    ShellHelper.Start(dsres.ReleaseUrl);
                }
            }
        }

        private void InfoPanelSizeChanged(object sender, EventArgs e)
        {
            infoPanel.Left = (resultsDataGridView.Width - infoPanel.Width) / 2;
        }

        private void FrmSearchShown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }
    }

    public class SourceComparer : IComparer<ISearchResult>
    {
        public int Compare(ISearchResult x, ISearchResult y)
        {
            var hsrX = x as HubSearchResult;
            var hsrY = y as HubSearchResult;

            if (hsrX == null && hsrY == null)
                return 0;
            if (hsrX == null)
                return -1;
            if (hsrY == null)
                return 1;
            
            return hsrY.Sources.Count.CompareTo(hsrX.Sources.Count);
        }
    }

    public class SourceComparerAsc : IComparer<ISearchResult>
    {
        public int Compare(ISearchResult x, ISearchResult y)
        {
            var hsrX = x as HubSearchResult;
            var hsrY = y as HubSearchResult;

            if (hsrX == null && hsrY == null)
                return 0;
            if (hsrX == null)
                return 1;
            if (hsrY == null)
                return -1;

            return hsrX.Sources.Count.CompareTo(hsrY.Sources.Count);
        }
    }

    public class SizeComparer : IComparer<ISearchResult>
    {
        public int Compare(ISearchResult x, ISearchResult y)
        {
            return y.Size.CompareTo(x.Size);
        }
    }

    public class SizeComparerAsc : IComparer<ISearchResult>
    {
        public int Compare(ISearchResult x, ISearchResult y)
        {
            return x.Size.CompareTo(y.Size);
        }
    }

    public class NameComparer : IComparer<ISearchResult>
    {
        public int Compare(ISearchResult x, ISearchResult y)
        {
            return y.Name.CompareTo(x.Name);
        }
    }

    public class NameComparerAsc : IComparer<ISearchResult>
    {
        public int Compare(ISearchResult x, ISearchResult y)
        {
            return x.Name.CompareTo(y.Name);
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


using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LiveDc.Forms;
using LiveDc.Helpers;
using LiveDc.Providers;
using LiveDc.Windows;
using SharpDc.Interfaces;
using SharpDc.Structs;

namespace LiveDc.Notify
{
    public partial class FrmNotify : Form
    {
        private readonly LiveClient _client;
        private FrmSearch _searchForm;

        public void AddItem(Magnet item, DateTime createDate)
        {
            historyLabel.Visible = false;

            if (flowLayoutPanel1.Controls.Count > 2)
            {
                flowLayoutPanel1.Controls.RemoveAt(2);
            }

            var dcItem = new DcFileControl()
            {
                Name = item.TTH,
                Magnet = item,
                CreateDate = createDate
            };

            dcItem.Icon = NativeImageList.TryGetLargeIcon(Path.GetExtension(dcItem.Magnet.FileName));
            dcItem.ContextMenuStrip = contextMenuStrip1;
            dcItem.DoubleClick += dcItem_DoubleClick;

            flowLayoutPanel1.Controls.Add(dcItem);
        }
        
        public FrmNotify(LiveClient client)
        {
            _client = client;
            InitializeComponent();
            
            NativeImageList.LargeExtensionImageLoaded += NativeImageListLargeExtensionImageLoaded;

            timer1.Tick += Timer1Tick;
            timer1.Interval = 1000;
            timer1.Start();

            Activated += FrmNotify_Activated;

            label1.Text = "LiveDC " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        }

        void FrmNotify_Activated(object sender, EventArgs e)
        {
            RefreshItems();
        }

        private void RefreshItems()
        {
            foreach (DcFileControl control in flowLayoutPanel1.Controls)
            {
                control.DoubleClick -= dcItem_DoubleClick;
                control.ContextMenuStrip = null;
            }

            flowLayoutPanel1.Controls.Clear();
            historyLabel.Visible = true;

            if (_client.History != null)
            {
                foreach (var hItem in _client.History.Items().Take(3))
                {
                    AddItem(hItem.Magnet, hItem.CreateDate);
                }

                UpdateItems();
            }
        }

        private void UpdateItems()
        {
            if (_client == null)
                return;

            foreach (DcFileControl control in flowLayoutPanel1.Controls)
            {
                _client.UpdateFileItem(control);
                control.Invalidate();
            }
        }

        void Timer1Tick(object sender, EventArgs e)
        {
            UpdateItems();
        }

        void NativeImageListLargeExtensionImageLoaded(object sender, NativeImageListEventArgs e)
        {
            foreach (DcFileControl item in flowLayoutPanel1.Controls)
            {
                if (Path.GetExtension(item.Magnet.FileName) == e.Extension)
                    item.Icon = e.Icon;
            }
        }

        public void DrawVisualStyleElementTaskbarBackgroundBottom1(PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MinimumSize = this.MaximumSize = this.Size;
            this.Text = "";
        }

        const int WM_NCHITTEST = 0x0084;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTTOP = 12;
        const int HTCLIENT = 1;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
            {
                switch (m.Result.ToInt32())
                {
                    case HTBOTTOM:
                    case HTBOTTOMLEFT:
                    case HTBOTTOMRIGHT:
                    case HTLEFT:
                    case HTRIGHT:
                    case HTTOPLEFT:
                    case HTTOPRIGHT:
                    case HTTOP:
                        m.Result = (IntPtr)HTCLIENT;
                        break;
                }
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawVisualStyleElementTaskbarBackgroundBottom1(e);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            
        }

        private void Form1_Activated(object sender, EventArgs e)
        {

        }

        public void UpdateWindowPos(NotifyIcon icon)
        {
            bool glassenabled = Compatibility.IsDWMEnabled;

            //// update location

            Rectangle windowbounds = (glassenabled ? WindowPositioning.GetWindowSize(Handle) : WindowPositioning.GetWindowClientAreaSize(Handle));

            Graphics g = this.CreateGraphics();


            double dpiX = g.DpiX / 96f; // screenmatrix.M11; // 1.0 = 96 dpi
            double dpiY = g.DpiY / 96f; // screenmatrix.M22; // 1.25 = 120 dpi, etc.

            Point position = WindowPositioning.GetWindowPosition(icon, windowbounds.Width, windowbounds.Height, dpiX, false);

            // translate wpf points to screen coordinates
            Point screenposition = new Point((int)(position.X / dpiX), (int)(position.Y / dpiY));

            this.Left = screenposition.X;
            this.Top = screenposition.Y;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (VisualStyleRenderer.IsElementDefined(VisualStyleElement.Status.Bar.Normal) && Environment.OSVersion.Version.ToString(2) == "6.2")
            {
                VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.Status.Bar.Normal);
                renderer.DrawBackground(e.Graphics, panel1.ClientRectangle);
            }
            else
            {
                var startColor = Color.FromArgb(241, 245, 251);
                var endColor = Color.FromArgb(204, 217, 234);

                using (var brGradient = new LinearGradientBrush(panel1.ClientRectangle, startColor, endColor, LinearGradientMode.Vertical))
                {
                    brGradient.Blend = new Blend
                    {
                        Factors = new[] { 1.0f, 0.1f, 0.0f },
                        Positions = new[] { 0.0f, 0.1f, 1.0f }
                    };
                    e.Graphics.FillRectangle(brGradient, panel1.ClientRectangle);
                }
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            var rect = panel2.ClientRectangle;

            rect.Y = rect.Height - 1;
            rect.Height = 1;
            //new SolidBrush(Color.FromArgb(255, 190, 190, 160))
            e.Graphics.FillRectangle(Brushes.LightGray, rect);


        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(_client.Drive.DriveRoot);
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _client.StartFile(FromMenu(sender).Magnet);
        }

        private DcFileControl FromMenu(object menuItem)
        {
            var item = (ToolStripMenuItem)menuItem;

            var menu = (ContextMenuStrip)item.GetCurrentParent();

            return (DcFileControl)menu.SourceControl;
        }

        private void найтиВПапкеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = FromMenu(sender);

            var path = Path.Combine(_client.Drive.DriveRoot, item.Magnet.FileName);

            ShellHelper.FindFileInExplorer(path);
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = FromMenu(sender);

            _client.DeleteItem(item.Magnet);
            
            RefreshItems();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Подверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            new FrmSettings(_client).ShowDialog();
        }

        void dcItem_DoubleClick(object sender, EventArgs e)
        {
            var item = (DcFileControl)sender;
            _client.StartFile(item.Magnet);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (_searchForm == null)
            {
                _searchForm = new FrmSearch(_client, _client.Providers.OfType<DcProvider>().First());
                _searchForm.Closed += _searchForm_Closed;
            }

            _searchForm.Show();
            _searchForm.Activate();

        }

        void _searchForm_Closed(object sender, EventArgs e)
        {
            _searchForm.Closed -= _searchForm_Closed;
            _searchForm = null;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            var form = new FrmAttachUrl();

            if (form.ShowDialog() != DialogResult.OK) 
                return;

            try
            {
                var magnet = _client.HttpProvider.RegisterFile(form.Url);
                _client.StartFile(magnet);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

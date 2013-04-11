using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using NotifyIconSample;
using SharpDc.Structs;

namespace LiveDc.Notify
{
    public partial class FrmNotify : Form
    {
        public FrmNotify()
        {
            InitializeComponent();

            flowLayoutPanel1.Controls.Add(new DcFileControl() 
            { 
                Magnet = new Magnet("WTRHWRTHWRTHWRTHWTRHWTHWTRHWTR", 123000043, "game.of.thrones.s03e01.1080p.hdtv.Ac3.x264-got.mkv"),
                CreateDate = DateTime.Now
            });

            flowLayoutPanel1.Controls.Add(new DcFileControl()
            {
                Magnet = new Magnet("TRTWERTWERTWERTWERTWERTWERT", 321000043, "game.of.thrones.s03e03.1080p.hdtv.Ac3.x264-got.mkv"),
                CreateDate = DateTime.Now
            });

            NativeImageList.LargeExtensionImageLoaded += NativeImageList_LargeExtensionImageLoaded;

            foreach (DcFileControl control in flowLayoutPanel1.Controls)
            {
                control.Icon = NativeImageList.TryGetLargeIcon(Path.GetExtension(control.Magnet.FileName));
            }
        }

        void NativeImageList_LargeExtensionImageLoaded(object sender, NativeImageListEventArgs e)
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

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.Close();
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
    }
}

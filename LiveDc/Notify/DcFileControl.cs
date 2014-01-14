using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LiveDc.Helpers;
using SharpDc;
using SharpDc.Structs;

namespace LiveDc.Notify
{
    public class DcFileControl : Control
    {
        private bool _hover;
        private Image _icon;

        public Magnet Magnet { get; set; }

        public long DownloadSpeed { get; set; }

        public long DownloadedBytes { get; set; }

        public float Progress { get; set; }

        public DateTime CreateDate { get; set; }

        public string Status { get; set; }

        public Image Icon
        {
            get { return _icon; }
            set { 
                _icon = value;
                if (InvokeRequired)
                    Invoke(new ThreadStart(Invalidate));
                else
                {
                    Invalidate();
                }
            }
        }
        
        public DcFileControl()
        {
            this.Height = 40;
            this.Width = 100;
            SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.FixedHeight | ControlStyles.OptimizedDoubleBuffer, true );
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            if (Parent != null)
            {
                Size = new Size(Parent.Width - Parent.Margin.Left - Parent.Margin.Right, Height);
            }
            base.OnParentChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //e.Graphics.FillRectangle(Brushes.Red, ClientRectangle);
            
            //e.Graphics.DrawString(Magnet.FileName, Font, new SolidBrush(ForeColor), ClientRectangle.X + 5, ClientRectangle.Y + 5);
            
            // background

            if (_hover)
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), ClientRectangle);  

            if (Progress > 0 && Progress < 1)
            {
                var color = _hover ? Color.FromArgb(144, 255, 162) : Color.FromArgb(211, 255, 218);

                var rect = ClientRectangle;

                rect.Width = (int)(rect.Width * Progress);
                e.Graphics.FillRectangle(new SolidBrush(color), rect );
            }            
            
            if (Icon != null)
            {
                var iconRect = new Rectangle(5,5, 32, 32);
                e.Graphics.DrawImage(Icon, iconRect);
            }

            var fileNameRect = ClientRectangle;
            fileNameRect.X += 45;
            fileNameRect.Y += 5;
            fileNameRect.Width -= fileNameRect.X;
            fileNameRect.Height = 20;

            //e.Graphics.FillRectangle(Brushes.LightCoral, fileNameRect);

            e.Graphics.DrawString(FitFileName(e.Graphics, Magnet.FileName, fileNameRect), Font, Brushes.Black, fileNameRect);

            var infoRect = ClientRectangle;
            infoRect.X += 45;
            infoRect.Y += 22;
            infoRect.Width -= infoRect.X;
            infoRect.Height = 20;

            string infoText;

            if (DownloadSpeed == 0)
                infoText = string.Format("добавлен {0}{1}", TimeFormatHelper.Format(CreateDate), Progress == 1f ? "" : ", не загружен");
            else
            {
                infoText = string.Format("{0}% {1} {2}/c", (int)(Progress * 100), Utils.FormatBytes(Magnet.Size), Utils.FormatBytes(DownloadSpeed));
            }

            e.Graphics.DrawString(infoText, Font, Brushes.Gray, infoRect);
            
            base.OnPaint(e);
        }

        private string FitFileName(Graphics g, string fullName, Rectangle rect)
        {
            var totalSize = g.MeasureString(fullName, Font);

            if (totalSize.Width <= rect.Width)
                return fullName;

            var ext = Path.GetExtension(fullName);
            var name = Path.GetFileNameWithoutExtension(fullName);
            
            for (int i = name.Length - 1; i >= 0; i--)
            {
                var n = name.Substring(0, i) + "..." + ext;

                var curSize = g.MeasureString(n , Font);

                if (curSize.Width <= rect.Width)
                    return n;
            }

            return ext;
        }

    }
}

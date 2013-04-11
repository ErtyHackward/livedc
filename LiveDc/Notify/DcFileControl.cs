using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpDc.Structs;

namespace LiveDc.Notify
{
    public class DcFileControl : Control
    {
        public Magnet Magnet { get; set; }

        public long DownloadSpeed { get; set; }

        public long DownloadedBytes { get; set; }

        public float Progress { get { return (float)DownloadedBytes / Magnet.Size; } }

        public string FilePath { get; set; }

        public DateTime CreateDate { get; set; }

        public string Status { get; set; }

        public DcFileControl()
        {
            SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            


            
            base.OnPaint(e);
        }

    }
}

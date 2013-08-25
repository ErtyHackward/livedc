using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiveDc.Providers;
using MonoTorrent.Client;

namespace LiveDc.Forms
{
    public partial class SegementsControl : UserControl
    {
        private Pen readPen = new Pen(Color.Red, 2f);
        
        public TorrentManager Manager { get; set; }
        public List<TorrentStream> ReadStreams { get; private set; }

        public SegementsControl()
        {
            InitializeComponent();

            ReadStreams = new List<TorrentStream>();

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawBG(e.Graphics);
            if (Manager == null)
            {
                return;
            }

            var piecesCount = Manager.Torrent.Size / Manager.Torrent.PieceLength;

            if (Manager.Torrent.Size % Manager.Torrent.PieceLength > 0)
                piecesCount++;
            
            var pieceLengthControl = FilePosToControl(Manager.Torrent.PieceLength);

            var picker = Manager.PieceManager.GetPicker<StandardPicker>();
            var activePieces = picker != null ? picker.RequestedPieces().ToList() : null;

            int pieceIndex = 0;
            for (float x = 0; x < Width; x += pieceLengthControl)
            {
                var rect = new RectangleF(x, 0, pieceLengthControl, Height);

                if (Manager.Bitfield[pieceIndex])
                {
                    e.Graphics.FillRectangle(Brushes.Green, rect);
                    //e.Graphics.DrawRectangle(SystemPens.Gree, rect);
                }
                else
                {
                    if (activePieces != null && activePieces.Any(p => p.Index == pieceIndex))
                    {
                        e.Graphics.FillRectangle(Brushes.Yellow, rect);
                    }
                    else
                    {
                        e.Graphics.DrawRectangles(SystemPens.GradientActiveCaption, new[] { rect });    
                    }
                }

                pieceIndex++;
            }

            lock (ReadStreams)
            {
                foreach (var torrentStream in ReadStreams)
                {
                    try
                    {
                        var pos = FilePosToControl(torrentStream.FilePosToPiece(torrentStream.Position) * Manager.Torrent.PieceLength + torrentStream.Position % Manager.Torrent.PieceLength);
                        e.Graphics.DrawLine(readPen, pos, 1, pos, Height);
                    }
                    catch (ObjectDisposedException ex)
                    {

                    }
                }
            }
            
            //DrawBG(e.Graphics);
            base.OnPaint(e);
        }

        private float FilePosToControl(long filePos)
        {
            return Width * ((float)filePos / Manager.Torrent.Size);
        }

        private void DrawBG(Graphics g)
        {
            var innerRect = ClientRectangle;

            innerRect.Width--;
            innerRect.Height--;

            g.FillRectangle(SystemBrushes.Control, innerRect);
            g.DrawRectangle(SystemPens.ControlDarkDark, innerRect);
        }
    }
}

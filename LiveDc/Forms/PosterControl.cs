using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LiveDc.Forms
{
    public partial class PosterControl : UserControl
    {
        public override string Text { 
            get { return label1.Text; } 
            set { label1.Text = value; } 
        }

        public Image Poster { 
            get { return pictureBox1.Image; } 
            set { pictureBox1.Image = value; } 
        }

        public PosterControl()
        {
            InitializeComponent();

            pictureBox1.Click += (sender, args) => OnClick(args);
            label1.Click += (sender, args) => OnClick(args);

            
        }

        public override Cursor Cursor
        {
            get
            {
                return base.Cursor;
            }
            set
            {
                base.Cursor = value;
                pictureBox1.Cursor = value;
                label1.Cursor = value;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
        }
    }
}

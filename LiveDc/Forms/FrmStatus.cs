using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace LiveDc.Forms
{
    public partial class FrmStatus : Form
    {
        PrivateFontCollection _fontCollection;

        public string MainText
        {
            get { return label2.Text; }
            set { label2.Text = value; }
        }

        public string StatusText
        {
            get { return label3.Text; }
            set { label3.Text = value; }
        }
        
        public FrmStatus()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }
    }
}

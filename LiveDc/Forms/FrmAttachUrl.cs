using System;
using System.Windows.Forms;

namespace LiveDc.Forms
{
    public partial class FrmAttachUrl : Form
    {
        public string Url { get; set; }

        public FrmAttachUrl()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            Url = textBox1.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LiveDc.Forms
{
    public partial class FrmDownloadDebug : Form
    {
        public FrmDownloadDebug()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            SegementsControl.Refresh();
        }
    }
}
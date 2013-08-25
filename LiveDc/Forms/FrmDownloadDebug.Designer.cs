namespace LiveDc.Forms
{
    partial class FrmDownloadDebug
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SegementsControl = new LiveDc.Forms.SegementsControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // SegementsControl
            // 
            this.SegementsControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SegementsControl.Location = new System.Drawing.Point(13, 13);
            this.SegementsControl.Manager = null;
            this.SegementsControl.Name = "SegementsControl";
            this.SegementsControl.Size = new System.Drawing.Size(1829, 58);
            this.SegementsControl.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // FrmDownloadDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1854, 93);
            this.Controls.Add(this.SegementsControl);
            this.Location = new System.Drawing.Point(0, 300);
            this.Name = "FrmDownloadDebug";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "FrmDownloadDebug";
            this.ResumeLayout(false);

        }

        #endregion

        public SegementsControl SegementsControl;
        private System.Windows.Forms.Timer timer1;

    }
}
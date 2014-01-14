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
            this.label1 = new System.Windows.Forms.Label();
            this.downloadSpeedLabel = new System.Windows.Forms.Label();
            this.peersLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SegementsControl
            // 
            this.SegementsControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SegementsControl.Location = new System.Drawing.Point(13, 30);
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Segments:";
            // 
            // downloadSpeedLabel
            // 
            this.downloadSpeedLabel.AutoSize = true;
            this.downloadSpeedLabel.Location = new System.Drawing.Point(13, 95);
            this.downloadSpeedLabel.Name = "downloadSpeedLabel";
            this.downloadSpeedLabel.Size = new System.Drawing.Size(115, 13);
            this.downloadSpeedLabel.TabIndex = 2;
            this.downloadSpeedLabel.Text = "Total download speed:";
            // 
            // peersLabel
            // 
            this.peersLabel.AutoSize = true;
            this.peersLabel.Location = new System.Drawing.Point(13, 112);
            this.peersLabel.Name = "peersLabel";
            this.peersLabel.Size = new System.Drawing.Size(104, 13);
            this.peersLabel.TabIndex = 3;
            this.peersLabel.Text = "Total peers: 0 fast: 0";
            // 
            // FrmDownloadDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1854, 211);
            this.Controls.Add(this.peersLabel);
            this.Controls.Add(this.downloadSpeedLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SegementsControl);
            this.Location = new System.Drawing.Point(0, 300);
            this.Name = "FrmDownloadDebug";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "FrmDownloadDebug";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public SegementsControl SegementsControl;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label downloadSpeedLabel;
        private System.Windows.Forms.Label peersLabel;

    }
}
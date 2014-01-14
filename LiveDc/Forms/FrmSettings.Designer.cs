namespace LiveDc.Forms
{
    partial class FrmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            this.cancelButton = new System.Windows.Forms.Button();
            this.applyButton = new System.Windows.Forms.Button();
            this.storagePathText = new System.Windows.Forms.TextBox();
            this.storageSelectButton = new System.Windows.Forms.Button();
            this.storageAutoselectCheck = new System.Windows.Forms.CheckBox();
            this.storageAutopruneCheck = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.udpPortNumeric = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.tcpPortNumeric = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.torrentAssocCheck = new System.Windows.Forms.CheckBox();
            this.magnetAssocCheck = new System.Windows.Forms.CheckBox();
            this.autostartCheck = new System.Windows.Forms.CheckBox();
            this.startPageUrlText = new System.Windows.Forms.TextBox();
            this.startPageCheck = new System.Windows.Forms.CheckBox();
            this.autoupdateCheck = new System.Windows.Forms.CheckBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tcpPortNumeric)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(407, 345);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Отмена";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // applyButton
            // 
            this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.applyButton.Location = new System.Drawing.Point(326, 345);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 1;
            this.applyButton.Text = "Принять";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // storagePathText
            // 
            this.storagePathText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.storagePathText.Location = new System.Drawing.Point(6, 26);
            this.storagePathText.Name = "storagePathText";
            this.storagePathText.Size = new System.Drawing.Size(389, 20);
            this.storagePathText.TabIndex = 4;
            // 
            // storageSelectButton
            // 
            this.storageSelectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.storageSelectButton.Location = new System.Drawing.Point(401, 25);
            this.storageSelectButton.Name = "storageSelectButton";
            this.storageSelectButton.Size = new System.Drawing.Size(75, 22);
            this.storageSelectButton.TabIndex = 5;
            this.storageSelectButton.Text = "Выбрать";
            this.storageSelectButton.UseVisualStyleBackColor = true;
            this.storageSelectButton.Click += new System.EventHandler(this.storageSelectButton_Click);
            // 
            // storageAutoselectCheck
            // 
            this.storageAutoselectCheck.AutoSize = true;
            this.storageAutoselectCheck.Location = new System.Drawing.Point(6, 52);
            this.storageAutoselectCheck.Name = "storageAutoselectCheck";
            this.storageAutoselectCheck.Size = new System.Drawing.Size(82, 17);
            this.storageAutoselectCheck.TabIndex = 6;
            this.storageAutoselectCheck.Text = "Автовыбор";
            this.toolTip1.SetToolTip(this.storageAutoselectCheck, "Программа автоматически выберет наиболее свободный жесткий диск");
            this.storageAutoselectCheck.UseVisualStyleBackColor = true;
            this.storageAutoselectCheck.Click += new System.EventHandler(this.storageAutoselectCheck_Click);
            // 
            // storageAutopruneCheck
            // 
            this.storageAutopruneCheck.AutoSize = true;
            this.storageAutopruneCheck.Location = new System.Drawing.Point(6, 75);
            this.storageAutopruneCheck.Name = "storageAutopruneCheck";
            this.storageAutopruneCheck.Size = new System.Drawing.Size(274, 17);
            this.storageAutopruneCheck.TabIndex = 8;
            this.storageAutopruneCheck.Text = "Удалять старые загрузки при недостатке места";
            this.toolTip1.SetToolTip(this.storageAutopruneCheck, "Система удалит старые файлы для освобождения места для новых. Удаляются только те" +
        " файлы, что были загружены программой.");
            this.storageAutopruneCheck.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Загружать файлы в:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.udpPortNumeric);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.tcpPortNumeric);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Location = new System.Drawing.Point(6, 6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(470, 78);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Соединения";
            // 
            // udpPortNumeric
            // 
            this.udpPortNumeric.Location = new System.Drawing.Point(77, 45);
            this.udpPortNumeric.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
            this.udpPortNumeric.Name = "udpPortNumeric";
            this.udpPortNumeric.Size = new System.Drawing.Size(61, 20);
            this.udpPortNumeric.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 47);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Порт UDP";
            // 
            // tcpPortNumeric
            // 
            this.tcpPortNumeric.Location = new System.Drawing.Point(77, 19);
            this.tcpPortNumeric.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
            this.tcpPortNumeric.Name = "tcpPortNumeric";
            this.tcpPortNumeric.Size = new System.Drawing.Size(61, 20);
            this.tcpPortNumeric.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Порт TCP";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(2, 6);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(492, 333);
            this.tabControl1.TabIndex = 12;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.torrentAssocCheck);
            this.tabPage1.Controls.Add(this.magnetAssocCheck);
            this.tabPage1.Controls.Add(this.autostartCheck);
            this.tabPage1.Controls.Add(this.startPageUrlText);
            this.tabPage1.Controls.Add(this.startPageCheck);
            this.tabPage1.Controls.Add(this.autoupdateCheck);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(484, 307);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Главное";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // torrentAssocCheck
            // 
            this.torrentAssocCheck.AutoSize = true;
            this.torrentAssocCheck.Location = new System.Drawing.Point(6, 99);
            this.torrentAssocCheck.Name = "torrentAssocCheck";
            this.torrentAssocCheck.Size = new System.Drawing.Size(178, 17);
            this.torrentAssocCheck.TabIndex = 14;
            this.torrentAssocCheck.Text = "Ассоциация с torrent файлами";
            this.torrentAssocCheck.UseVisualStyleBackColor = true;
            // 
            // magnetAssocCheck
            // 
            this.magnetAssocCheck.AutoSize = true;
            this.magnetAssocCheck.Location = new System.Drawing.Point(6, 76);
            this.magnetAssocCheck.Name = "magnetAssocCheck";
            this.magnetAssocCheck.Size = new System.Drawing.Size(190, 17);
            this.magnetAssocCheck.TabIndex = 13;
            this.magnetAssocCheck.Text = "Ассоциация с магнет-ссылками";
            this.magnetAssocCheck.UseVisualStyleBackColor = true;
            // 
            // autostartCheck
            // 
            this.autostartCheck.AutoSize = true;
            this.autostartCheck.Location = new System.Drawing.Point(6, 6);
            this.autostartCheck.Name = "autostartCheck";
            this.autostartCheck.Size = new System.Drawing.Size(191, 17);
            this.autostartCheck.TabIndex = 9;
            this.autostartCheck.Text = "Автозапуск при старте системы";
            this.autostartCheck.UseVisualStyleBackColor = true;
            // 
            // startPageUrlText
            // 
            this.startPageUrlText.Location = new System.Drawing.Point(188, 52);
            this.startPageUrlText.Name = "startPageUrlText";
            this.startPageUrlText.Size = new System.Drawing.Size(271, 20);
            this.startPageUrlText.TabIndex = 12;
            // 
            // startPageCheck
            // 
            this.startPageCheck.AutoSize = true;
            this.startPageCheck.Location = new System.Drawing.Point(6, 53);
            this.startPageCheck.Name = "startPageCheck";
            this.startPageCheck.Size = new System.Drawing.Size(176, 17);
            this.startPageCheck.TabIndex = 11;
            this.startPageCheck.Text = "При запуске открывать сайт:";
            this.startPageCheck.UseVisualStyleBackColor = true;
            // 
            // autoupdateCheck
            // 
            this.autoupdateCheck.AutoSize = true;
            this.autoupdateCheck.Checked = true;
            this.autoupdateCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoupdateCheck.Enabled = false;
            this.autoupdateCheck.Location = new System.Drawing.Point(6, 29);
            this.autoupdateCheck.Name = "autoupdateCheck";
            this.autoupdateCheck.Size = new System.Drawing.Size(219, 17);
            this.autoupdateCheck.TabIndex = 10;
            this.autoupdateCheck.Text = "Автоматически обновлять программу";
            this.autoupdateCheck.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.storageAutopruneCheck);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.storageAutoselectCheck);
            this.tabPage2.Controls.Add(this.storagePathText);
            this.tabPage2.Controls.Add(this.storageSelectButton);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(484, 307);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Хранилище";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox3);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(484, 307);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Сеть";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // FrmSettings
            // 
            this.AcceptButton = this.applyButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(494, 380);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSettings";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Настройки";
            this.Load += new System.EventHandler(this.FrmSettings_Load);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tcpPortNumeric)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.TextBox storagePathText;
        private System.Windows.Forms.Button storageSelectButton;
        private System.Windows.Forms.CheckBox storageAutoselectCheck;
        private System.Windows.Forms.CheckBox storageAutopruneCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.NumericUpDown udpPortNumeric;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown tcpPortNumeric;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.CheckBox torrentAssocCheck;
        private System.Windows.Forms.CheckBox magnetAssocCheck;
        private System.Windows.Forms.CheckBox autostartCheck;
        private System.Windows.Forms.TextBox startPageUrlText;
        private System.Windows.Forms.CheckBox startPageCheck;
        private System.Windows.Forms.CheckBox autoupdateCheck;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
    }
}
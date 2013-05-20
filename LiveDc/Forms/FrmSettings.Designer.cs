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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.storageAutopruneCheck = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.idleEconomyCheck = new System.Windows.Forms.CheckBox();
            this.autostartCheck = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.autoupdateCheck = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tcpPortNumeric = new System.Windows.Forms.NumericUpDown();
            this.udpPortNumeric = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tcpPortNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortNumeric)).BeginInit();
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
            this.storagePathText.Location = new System.Drawing.Point(6, 36);
            this.storagePathText.Name = "storagePathText";
            this.storagePathText.Size = new System.Drawing.Size(453, 20);
            this.storagePathText.TabIndex = 4;
            // 
            // storageSelectButton
            // 
            this.storageSelectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.storageSelectButton.Location = new System.Drawing.Point(384, 62);
            this.storageSelectButton.Name = "storageSelectButton";
            this.storageSelectButton.Size = new System.Drawing.Size(75, 23);
            this.storageSelectButton.TabIndex = 5;
            this.storageSelectButton.Text = "Выбрать";
            this.storageSelectButton.UseVisualStyleBackColor = true;
            this.storageSelectButton.Click += new System.EventHandler(this.storageSelectButton_Click);
            // 
            // storageAutoselectCheck
            // 
            this.storageAutoselectCheck.AutoSize = true;
            this.storageAutoselectCheck.Location = new System.Drawing.Point(6, 62);
            this.storageAutoselectCheck.Name = "storageAutoselectCheck";
            this.storageAutoselectCheck.Size = new System.Drawing.Size(82, 17);
            this.storageAutoselectCheck.TabIndex = 6;
            this.storageAutoselectCheck.Text = "Автовыбор";
            this.toolTip1.SetToolTip(this.storageAutoselectCheck, "Программа автоматически выберет наиболее свободный жесткий диск");
            this.storageAutoselectCheck.UseVisualStyleBackColor = true;
            this.storageAutoselectCheck.Click += new System.EventHandler(this.storageAutoselectCheck_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.storageAutopruneCheck);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.storageAutoselectCheck);
            this.groupBox1.Controls.Add(this.storageSelectButton);
            this.groupBox1.Controls.Add(this.storagePathText);
            this.groupBox1.Location = new System.Drawing.Point(12, 118);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(469, 115);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Хранилище";
            // 
            // storageAutopruneCheck
            // 
            this.storageAutopruneCheck.AutoSize = true;
            this.storageAutopruneCheck.Enabled = false;
            this.storageAutopruneCheck.Location = new System.Drawing.Point(6, 85);
            this.storageAutopruneCheck.Name = "storageAutopruneCheck";
            this.storageAutopruneCheck.Size = new System.Drawing.Size(325, 17);
            this.storageAutopruneCheck.TabIndex = 8;
            this.storageAutopruneCheck.Text = "Автоматически очищать хранилище при недостатке места";
            this.toolTip1.SetToolTip(this.storageAutopruneCheck, "Система удалит старые файлы для освобождения места для новых. Удаляются только те" +
        " файлы, что были загружены программой.");
            this.storageAutopruneCheck.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Загружать файлы в:";
            // 
            // idleEconomyCheck
            // 
            this.idleEconomyCheck.AutoSize = true;
            this.idleEconomyCheck.Enabled = false;
            this.idleEconomyCheck.Location = new System.Drawing.Point(6, 42);
            this.idleEconomyCheck.Name = "idleEconomyCheck";
            this.idleEconomyCheck.Size = new System.Drawing.Size(302, 17);
            this.idleEconomyCheck.TabIndex = 8;
            this.idleEconomyCheck.Text = "Пониженное потребление ресурсов во время простоя";
            this.toolTip1.SetToolTip(this.idleEconomyCheck, "Во время простоя программа будет ограничивать скорость отдачи контента");
            this.idleEconomyCheck.UseVisualStyleBackColor = true;
            // 
            // autostartCheck
            // 
            this.autostartCheck.AutoSize = true;
            this.autostartCheck.Location = new System.Drawing.Point(6, 19);
            this.autostartCheck.Name = "autostartCheck";
            this.autostartCheck.Size = new System.Drawing.Size(191, 17);
            this.autostartCheck.TabIndex = 9;
            this.autostartCheck.Text = "Автозапуск при старте системы";
            this.autostartCheck.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.autoupdateCheck);
            this.groupBox2.Controls.Add(this.autostartCheck);
            this.groupBox2.Controls.Add(this.idleEconomyCheck);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(469, 100);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Главное";
            // 
            // autoupdateCheck
            // 
            this.autoupdateCheck.AutoSize = true;
            this.autoupdateCheck.Checked = true;
            this.autoupdateCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoupdateCheck.Enabled = false;
            this.autoupdateCheck.Location = new System.Drawing.Point(6, 66);
            this.autoupdateCheck.Name = "autoupdateCheck";
            this.autoupdateCheck.Size = new System.Drawing.Size(219, 17);
            this.autoupdateCheck.TabIndex = 10;
            this.autoupdateCheck.Text = "Автоматически обновлять программу";
            this.autoupdateCheck.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.udpPortNumeric);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.tcpPortNumeric);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Location = new System.Drawing.Point(12, 240);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(470, 78);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Соединения";
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
            // FrmSettings
            // 
            this.AcceptButton = this.applyButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(494, 380);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
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
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tcpPortNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortNumeric)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.TextBox storagePathText;
        private System.Windows.Forms.Button storageSelectButton;
        private System.Windows.Forms.CheckBox storageAutoselectCheck;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox idleEconomyCheck;
        private System.Windows.Forms.CheckBox autostartCheck;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox autoupdateCheck;
        private System.Windows.Forms.CheckBox storageAutopruneCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.NumericUpDown udpPortNumeric;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown tcpPortNumeric;
        private System.Windows.Forms.Label label2;
    }
}
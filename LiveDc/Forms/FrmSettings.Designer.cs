﻿namespace LiveDc.Forms
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
            this.cancelButton = new System.Windows.Forms.Button();
            this.applyButton = new System.Windows.Forms.Button();
            this.storagePathText = new System.Windows.Forms.TextBox();
            this.storageSelectButton = new System.Windows.Forms.Button();
            this.storageAutoselectCheck = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.idleEconomyCheck = new System.Windows.Forms.CheckBox();
            this.autostartCheck = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.autoupdateCheck = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.storageAutopruneCheck = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(558, 258);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Отмена";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // applyButton
            // 
            this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.applyButton.Location = new System.Drawing.Point(477, 258);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 1;
            this.applyButton.Text = "Принять";
            this.applyButton.UseVisualStyleBackColor = true;
            // 
            // storagePathText
            // 
            this.storagePathText.Location = new System.Drawing.Point(6, 36);
            this.storagePathText.Name = "storagePathText";
            this.storagePathText.Size = new System.Drawing.Size(605, 20);
            this.storagePathText.TabIndex = 4;
            // 
            // storageSelectButton
            // 
            this.storageSelectButton.Location = new System.Drawing.Point(536, 62);
            this.storageSelectButton.Name = "storageSelectButton";
            this.storageSelectButton.Size = new System.Drawing.Size(75, 23);
            this.storageSelectButton.TabIndex = 5;
            this.storageSelectButton.Text = "Выбрать";
            this.storageSelectButton.UseVisualStyleBackColor = true;
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
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.storageAutopruneCheck);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.storageAutoselectCheck);
            this.groupBox1.Controls.Add(this.storageSelectButton);
            this.groupBox1.Controls.Add(this.storagePathText);
            this.groupBox1.Location = new System.Drawing.Point(12, 118);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(621, 115);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Хранилище";
            // 
            // idleEconomyCheck
            // 
            this.idleEconomyCheck.AutoSize = true;
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
            this.groupBox2.Controls.Add(this.autoupdateCheck);
            this.groupBox2.Controls.Add(this.autostartCheck);
            this.groupBox2.Controls.Add(this.idleEconomyCheck);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(621, 100);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Главное";
            // 
            // autoupdateCheck
            // 
            this.autoupdateCheck.AutoSize = true;
            this.autoupdateCheck.Location = new System.Drawing.Point(6, 66);
            this.autoupdateCheck.Name = "autoupdateCheck";
            this.autoupdateCheck.Size = new System.Drawing.Size(219, 17);
            this.autoupdateCheck.TabIndex = 10;
            this.autoupdateCheck.Text = "Автоматически обновлять программу";
            this.autoupdateCheck.UseVisualStyleBackColor = true;
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
            // storageAutopruneCheck
            // 
            this.storageAutopruneCheck.AutoSize = true;
            this.storageAutopruneCheck.Location = new System.Drawing.Point(6, 85);
            this.storageAutopruneCheck.Name = "storageAutopruneCheck";
            this.storageAutopruneCheck.Size = new System.Drawing.Size(325, 17);
            this.storageAutopruneCheck.TabIndex = 8;
            this.storageAutopruneCheck.Text = "Автоматически очищать хранилище при недостатке места";
            this.toolTip1.SetToolTip(this.storageAutopruneCheck, "Система удалит старые файлы для освобождения места для новых. Удаляются только те" +
        " файлы, что были загружены программой.");
            this.storageAutopruneCheck.UseVisualStyleBackColor = true;
            // 
            // FrmSettings
            // 
            this.AcceptButton = this.applyButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(645, 293);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSettings";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройки";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
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
    }
}
﻿using System;
using System.Windows.Forms;
using LiveDc.Properties;
using SharpDc.Connections;

namespace LiveDc.Forms
{
    public partial class FrmHubList : Form
    {
        private readonly LiveClient _client;

        private HubConnection _hub;

        public FrmHubList(LiveClient client)
        {
            Icon = Resources.livedc;
            _client = client;
            InitializeComponent();
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hubText.Text))
            {
                MessageBox.Show(
                    "Пожалуйста, введите адрес хаба для подключения. Скорее всего, эту информацию вы сможете найти на сайте вашего провайдера либо на форуме техподдержки.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _hub = _client.Engine.Hubs.Add(hubText.Text, _client.Settings.Nickname);

            _hub.ConnectionStatusChanged += HubConnectionStatusChanged;
            _hub.ActiveStatusChanged += HubActiveStatusChanged;

            addButton.Enabled = false;
            hubText.Enabled = false;

            _hub.ConnectAsync();
        }

        void HubActiveStatusChanged(object sender, EventArgs e)
        {
            if (_hub.Active)
            {
                if (_client.Settings.Hubs == null)
                {
                    _client.Settings.Hubs = "";
                }
                else 
                    _client.Settings.Hubs += ";";

                _client.Settings.Hubs += hubText.Text;
                _client.Settings.Save();

                _client.AsyncOperation.Post((o) =>
                                                {
                                                    statusLabel.Text = "Успешное подключение. Клиент готов к работе. Если желаете, можно добавить еще.";
                                                    continueButton.Enabled = true;
                                                }, null);
                CleanUpHub();
            }
        }

        void HubConnectionStatusChanged(object sender, SharpDc.Events.ConnectionStatusEventArgs e)
        {
            if (e.Status == SharpDc.Events.ConnectionStatus.Disconnected)
            {
                _client.AsyncOperation.Post((o) =>
                {
                    statusLabel.Text = "Отключен. Не удается подключиться к хабу. Попробуйте другой адрес.\n";
                    if (e.Exception != null)
                        statusLabel.Text += e.Exception.Message;
                    addButton.Enabled = true;
                    hubText.Enabled = true;
                }, null);

                _client.Engine.Hubs.Remove(_hub);
                CleanUpHub();
            }

            if (e.Status == SharpDc.Events.ConnectionStatus.Connecting)
            {
                _client.AsyncOperation.Post((o) =>
                {
                    statusLabel.Text = "Подключение...";
                }, null);
            }

            if (e.Status == SharpDc.Events.ConnectionStatus.Connecting)
            {
                _client.AsyncOperation.Post((o) =>
                {
                    statusLabel.Text = "Соединение установлено. Проверка хаба...";
                }, null);
            }
        }

        private void CleanUpHub()
        {
            _hub.ConnectionStatusChanged -= HubConnectionStatusChanged;
            _hub.ActiveStatusChanged -= HubActiveStatusChanged;
            _hub = null;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
            _client.Dispose();
            Application.Exit();
        }

        private void continueButton_Click(object sender, EventArgs e)
        {
            _client.Settings.Save();
            Close();
        }
    }
}
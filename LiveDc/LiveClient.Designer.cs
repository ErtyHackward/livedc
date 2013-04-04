using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiveDc.Properties;

namespace LiveDc
{
    public partial class LiveClient
    {
        public void InitializeComponent()
        {
            _icon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                Visible = true,
                Text = "Статус: отключен"
            };

            var appMenu = new ContextMenuStrip();
            appMenu.Items.Add("Выход").Click += ProgramExitClick;
            appMenu.Items.Add("Настройки").Click += SettingsClick;

            _icon.ContextMenuStrip = appMenu;
        }
    }
}

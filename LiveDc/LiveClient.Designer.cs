using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiveDc.Notify;
using LiveDc.Properties;

namespace LiveDc
{
    public partial class LiveClient
    {
        private static FrmNotify _form;
        
        public void InitializeComponent()
        {
            _icon = new NotifyIcon
            {
                Icon = Resources.livedc_offline,
                Visible = true,
                Text = "Статус: отключен"
            };

            _icon.MouseClick += _icon_MouseClick;

            var appMenu = new ContextMenuStrip();
            
            //appMenu.Items.Add("Настройки").Click += SettingsClick;
            appMenu.Items.Add("Выход").Click += ProgramExitClick;

            //_icon.ContextMenuStrip = appMenu;

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += TimerTick;
            _timer.Start();
        }
    }
}

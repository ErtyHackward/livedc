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
        private DateTime _hideTime;

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

        private void _icon_MouseClick(object sender, MouseEventArgs e)
        {
            if ((DateTime.Now - _hideTime).TotalMilliseconds < 300)
                return;

            if (_form == null)
            {
                _form = new FrmNotify(this);
                _form.Deactivate += form_Deactivate;
                _form.UpdateWindowPos(_icon);
                _form.Show();
                _form.Hide();
            }
            else
            {
                _form.UpdateWindowPos(_icon);
            }
                
            _form.Show();
            _form.Activate();
        }

        private void form_Deactivate(object sender, EventArgs e)
        {
            var form = (Form)sender;
            form.Hide();
            _hideTime = DateTime.Now;
        }
    }
}

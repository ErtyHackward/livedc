using System;
using System.Windows.Forms;
using LiveDc.Properties;

namespace LiveDc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NotifyIcon icon = new NotifyIcon();

            icon.Icon = Resources.AppIcon;
            icon.Visible = true;

            var appMenu = new ContextMenuStrip();
            appMenu.Items.Add("Exit").Click += delegate { icon.Visible = false; Application.Exit(); };
            icon.ContextMenuStrip = appMenu;

            LiveDcDrive drive = new LiveDcDrive();

            drive.MountAsync();

            Application.Run();
        }
    }
}

using System;
using System.Windows.Forms;
using LiveDc.Properties;
using QuickDc;

namespace LiveDc
{
    static class Program
    {
        public static DcEngine Engine;
        private static NotifyIcon _icon;
        private static LiveDcDrive _drive;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _icon = new NotifyIcon {
                Icon = Resources.AppIcon, 
                Visible = true, 
                Text = "Status: offline"
            };
            
            var appMenu = new ContextMenuStrip();
            appMenu.Items.Add("Exit").Click += ProgramExitClick;
            _icon.ContextMenuStrip = appMenu;

            _drive = new LiveDcDrive(Engine);
            _drive.MountAsync();

            var r = new Random();

            Engine = new DcEngine();

            Engine.Hubs.Add("hub2.o-go.ru", "livedc" + r.Next(0, 10000000));
            Engine.ActiveStatusChanged += EngineActivated;

            Engine.StartAsync();

            Application.Run();
        }

        static void ProgramExitClick(object sender, EventArgs e)
        {
            _drive.Unmount();
            Engine.Dispose(); 
            _icon.Visible = false; 
            Application.Exit();
        }

        static void EngineActivated(object sender, EventArgs e)
        {
            _icon.Text = Engine.Active ? "Status: online" : "Status: offline";
        }
    }
}

using System;
using System.Windows.Forms;
using LiveDc.Properties;
using LiveDc.Utilites;
using QuickDc;
using QuickDc.Managers;
using QuickDc.Structs;

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
        static void Main(string[] args)
        {
            #region Cmd args parsing
            foreach (var arg in args)
            {
                if (arg == "-reg")
                {
                    // try to register magnet handler
                    if (VistaSecurity.IsAdmin())
                    {
                        var result = WindowsHelper.RegisterMagnetHandler();

                        if (!result)
                            MessageBox.Show("Unable to set the program as a magnet handler", "Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        VistaSecurity.RestartElevated(args);
                    }
                }
            }
            #endregion

            StartApp();
        }

        static void StartApp()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!WindowsHelper.IsMagnetHandlerAssigned)
            {
                if (MessageBox.Show("Would you like LiveDc to handle magnet links?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    VistaSecurity.StartElevated("-reg");
                }
            }

            _icon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                Visible = true,
                Text = "Status: offline"
            };

            var appMenu = new ContextMenuStrip();
            appMenu.Items.Add("Exit").Click += ProgramExitClick;
            _icon.ContextMenuStrip = appMenu;

            var r = new Random();

            Engine = new DcEngine();

            _drive = new LiveDcDrive(Engine);
            _drive.MountAsync();

            Engine.Hubs.Add("hub2.o-go.ru", "livedc" + r.Next(0, 10000000));
            Engine.ActiveStatusChanged += EngineActivated;

            Engine.StartAsync();



            //var magnet = new Magnet(@"magnet:?xt=urn:tree:tiger:JQDOIABICG4SEXIRU6FY7UCWZDDFELETQ65PQWY&xl=782245888&dn=%D0%A1%D0%B0%D0%BC%D0%BE%D0%B5%20%D0%A1%D0%BC%D0%B5%D1%88%D0%BD%D0%BE%D0%B5%20%D0%92%D0%B8%D0%B4%D0%B5%D0%BE%201.avi");

            //Engine.Share = new MemoryShare();

            //Engine.Share.AddFile(new ContentItem 
            //                        { 
            //                            SystemPath = @"C:\Share\funny 1.avi",
            //                            VirtualPath = @"/funny 1.avi", 
            //                            Magnet = magnet 
            //                        });



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

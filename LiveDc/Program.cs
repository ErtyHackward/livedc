using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Notify;
using LiveDc.Utilites;
using SharpDc.Structs;
using Win32;

namespace LiveDc
{
    static class Program
    {
        private static LiveClient _client;

        public static DateTime BestBefore = DateTime.Parse("2014-02-01");

        public static bool SilentMode = false;

        public static string StartMagnet;
        public static string StartTorrent;
        private static HookWindow _hook;

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
                            MessageBox.Show("Не удается установить обработчик магнет-ссылок", "Ошибка",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        VistaSecurity.RestartElevated(args);
                    }
                    return;
                }

                if (arg == "-createshortcut")
                {
                    WindowsHelper.ShortcutToDesktop("LiveDC");
                    return;
                }

                if (arg == "-setstartup")
                {
                    if (VistaSecurity.IsAdmin())
                    {
                        var result = WindowsHelper.RunAtSystemStart(true);

                        if (!result)
                            MessageBox.Show("Не удается установить программу в автозагрузку", "Ошибка",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        VistaSecurity.RestartElevated(args);
                    }
                    return;
                }

                if (arg == "-removestartup")
                {
                    if (VistaSecurity.IsAdmin())
                    {
                        var result = WindowsHelper.RunAtSystemStart(false);

                        if (!result)
                            MessageBox.Show("Не удается удалить программу из автозагрузки", "Ошибка",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        VistaSecurity.RestartElevated(args);
                    }
                    return;
                }

                if (arg == "-autorun")
                {
                    SilentMode = true;
                }

                if (arg.StartsWith("magnet:"))
                {
                    Process proc = RunningInstance();
                    if (proc != null)
                    {
                        var m = new Magnet(arg);
                        using (var copyData = new CopyData())
                        {
                            copyData.Channels.Add("LIVEDC");
                            copyData.Channels["LIVEDC"].Send(m.ToString());
                        }
                        return;
                    }
                    StartMagnet = arg;
                }

                if (arg.EndsWith(".torrent"))
                {
                    Process proc = RunningInstance();
                    if (proc != null)
                    {
                        using (var copyData = new CopyData())
                        {
                            copyData.Channels.Add("LIVEDC");
                            copyData.Channels["LIVEDC"].Send(arg);
                        }
                        return;
                    }
                    StartTorrent = arg;
                }
            }
            #endregion

            Process otherProc = RunningInstance();

            if (otherProc != null)
            {
                using (var copyData = new CopyData())
                {
                    copyData.Channels.Add("LIVEDC");
                    copyData.Channels["LIVEDC"].Send("SHOW");
                }
                return;
            }

            if (DateTime.Now > BestBefore)
            {
                if (!SilentMode)
                {
                    if (MessageBox.Show(string.Format("Текущая версия клиента {0} устарела. Необходимо обновление. Перейти на сайт для загрузки?", Assembly.GetExecutingAssembly().GetName().Version.ToString(3)), "LiveDC", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Process.Start("http://april32.com/ru/products/livedc");
                    }
                }
                return;
            }

            StartApp();
        }

        static void StartApp()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _client = new LiveClient();

            _hook = new HookWindow();

            Application.Run();
        }

        public static Process RunningInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    if (Assembly.GetExecutingAssembly().Location.
                         Replace("/", "\\") == current.MainModule.FileName)
                    {
                        return process;
                    }
                }
            }
            return null;
        }


    }

    public sealed class HookWindow : NativeWindow
    {
        public HookWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            // Listen for operating system messages
            switch (m.Msg)
            {
                case NativeMethods.WM_CLOSE:
                    Application.Exit();
                    DestroyHandle();
                    break;
            }
            base.WndProc(ref m);
        }
    }
}

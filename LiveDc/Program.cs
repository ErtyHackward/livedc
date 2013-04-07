using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Utilites;
using SharpDc.Structs;
using Win32;

namespace LiveDc
{
    static class Program
    {
        private static LiveClient _client;

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
                    }
                    return;
                }
            }
            #endregion

            StartApp();
        }

        static void StartApp()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _client = new LiveClient();
            
            Application.Run();
        }

        public static Process RunningInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            //Loop through the running processes in with the same name 
            foreach (Process process in processes)
            {
                //Ignore the current process 
                if (process.Id != current.Id)
                {
                    //Make sure that the process is running from the exe file. 
                    if (Assembly.GetExecutingAssembly().Location.
                         Replace("/", "\\") == current.MainModule.FileName)
                    {
                        //Return the other process instance.  
                        return process;

                    }
                }
            }
            //No other instance was found, return null.  
            return null;
        }


    }
}

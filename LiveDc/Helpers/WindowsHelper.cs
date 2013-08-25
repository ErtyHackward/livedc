using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Windows.Forms;
using LiveDc.Windows;
using Microsoft.Win32;

namespace LiveDc.Helpers
{
    public class WindowsHelper
    {
        private static string protocol = "magnet";

        /// <summary>
        /// Indicates if the magnet links handler is created and assigned to the current application
        /// </summary>
        public static bool IsMagnetHandlerAssigned 
        {
            get
            {
                RegistryKey registry = Registry.CurrentUser;

                RegistryKey r;
                r = registry.OpenSubKey("SOFTWARE\\Classes\\" + protocol, false);
                if (r == null)
                    return false;

                r = registry.OpenSubKey("SOFTWARE\\Classes\\" + protocol + "\\shell\\open\\command", false);
                if (r == null)
                    return false;

                if (r.GetValue("").ToString() == Application.ExecutablePath + " \"%1\"")
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Register magnet links handler
        /// </summary>
        /// <returns>true if succeed, otherwise false</returns>
        public static bool RegisterMagnetHandler()
        {
            RegistryKey registry = Registry.CurrentUser;
            //RegistryKey cl = Registry.ClassesRoot.OpenSubKey(protocol);
            string application = Application.ExecutablePath;
            try
            {
                RegistryKey r;
                r = registry.OpenSubKey("SOFTWARE\\Classes\\" + protocol, true);
                if (r == null)
                    r = registry.CreateSubKey("SOFTWARE\\Classes\\" + protocol);
                r.SetValue("", "URL: P2P Magnet");
                r.SetValue("URL Protocol", "");

                r = registry.OpenSubKey("SOFTWARE\\Classes\\" + protocol + "\\DefaultIcon", true);
                if (r == null)
                    r = registry.CreateSubKey("SOFTWARE\\Classes\\" + protocol + "\\DefaultIcon");
                r.SetValue("", application);

                r = registry.OpenSubKey("SOFTWARE\\Classes\\" + protocol + "\\shell\\open\\command", true);
                if (r == null)
                    r = registry.CreateSubKey("SOFTWARE\\Classes\\" + protocol + "\\shell\\open\\command");

                r.SetValue("", application + " \"%1\"");


                // If 64-bit OS, also register in the 32-bit registry area. 
                if (registry.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes") != null)
                {
                    r = registry.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\" + protocol, true);
                    if (r == null)
                        r = registry.CreateSubKey("SOFTWARE\\Wow6432Node\\Classes\\" + protocol);
                    r.SetValue("", "URL: Protocol handled by CustomURL");
                    r.SetValue("URL Protocol", "");

                    r = registry.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\" + protocol + "\\DefaultIcon", true);
                    if (r == null)
                        r = registry.CreateSubKey("SOFTWARE\\Wow6432Node\\Classes\\" + protocol + "\\DefaultIcon");
                    r.SetValue("", application);

                    r = registry.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\" + protocol + "\\shell\\open\\command", true);
                    if (r == null)
                        r = registry.CreateSubKey("SOFTWARE\\Wow6432Node\\Classes\\" + protocol + "\\shell\\open\\command");

                    r.SetValue("", application + " \"%1\"");

                }
                return true;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (System.UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
        }


        public static bool RunAtSystemStart(bool value)
        {
            try
            {
                RegistryKey myKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run\", true);

                if (value)
                    myKey.SetValue(Application.ProductName, string.Format("\"{0}\" -autorun", Application.ExecutablePath));
                else
                    myKey.DeleteValue(Application.ProductName);
                return true;
            }
            catch { 
                return false;
            }
        }

        public static bool IsInAutoRun
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                    {
                        return key != null && key.GetValue(Application.ProductName) != null;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static bool IsWinVistaOrHigher
        {
            get
            {
                OperatingSystem OS = Environment.OSVersion;
                return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6);
            }
        }

        public static bool RunElevated(string programm, string cmdargs)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = programm;
            startInfo.Arguments = cmdargs;

            if (IsWinVistaOrHigher)
                startInfo.Verb = "runas";

            try
            {
                Process p = Process.Start(startInfo);
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        public static void ShortcutToDesktop(string linkName)
        {
            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            using (var writer = new StreamWriter(deskDir + "\\" + linkName + ".url"))
            {
                string app = System.Reflection.Assembly.GetExecutingAssembly().Location;
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + app);
                writer.WriteLine("IconIndex=0");
                string icon = app.Replace('\\', '/');
                writer.WriteLine("IconFile=" + icon);
                writer.Flush();
            }
        }

        public static bool GetProgramAssociatedWithExt(bool global, string ext, out string programName, out string programPath)
        {
            var key = global ? Registry.ClassesRoot : Registry.CurrentUser;
            var basePath = global ? "" : "SOFTWARE\\Classes\\";
            RegistryKey k;
            if (key.OpenSubKey(basePath + ext) == null)
            {
                programName = null;
                programPath = null;
                return false;
            }
            else
            {
                k = key.OpenSubKey(basePath + ext);
                programName = k.GetValue("").ToString();

                k = key.OpenSubKey(basePath + programName + "\\Shell\\Open\\Command");

                programPath = k.GetValue("").ToString();
                if (programPath.Contains(" "))
                    programPath = programPath.Remove(programPath.LastIndexOf(' '));

                programPath = programPath.Trim('"');
                return true;
            }
        }

        /// <param name="isGlobal"></param>
        /// <param name="programName"></param>
        /// <param name="extension">like .myext</param>
        /// <param name="executionFormat">like -"%1"</param>
        /// <param name="allowRollback">Does this extension can be rollback to old association</param>
        /// <returns></returns>
        public static bool RegisterExtension(bool isGlobal, string programName, string extension, string executionFormat = "", bool allowRollback = false)
        {
            if (string.IsNullOrEmpty(extension)) 
                throw new ArgumentNullException("extension");
            var key = isGlobal ? Registry.ClassesRoot : Registry.CurrentUser;
            string basePath = isGlobal ? "" : "SOFTWARE\\Classes\\";
            if (string.IsNullOrEmpty(executionFormat))
                executionFormat = "\"%1\"";
            try
            {
                RegistryKey r;
                if (key.OpenSubKey(basePath + programName) == null)
                {
                    r = key.CreateSubKey(basePath + programName);
                    r.SetValue("", programName);

                    r = key.CreateSubKey(basePath + programName + "\\Shell\\Open\\Command");
                    r.SetValue("", Application.ExecutablePath + " " + executionFormat);

                    // extension
                    if ((r = key.OpenSubKey(basePath + extension, true)) != null)
                    {
                        if (allowRollback)
                        {
                            var previous = r.GetValue("").ToString();

                            if (previous != programName)
                            {
                                // do rollback possibility
                                r.SetValue("Previous", r.GetValue(""));
                            }
                        }
                    }
                    else
                    {
                        r = key.CreateSubKey(basePath + extension);
                    }
                    r.SetValue("", programName);

                    r.Close();
                }
                else
                {
                    if ((r = key.OpenSubKey(basePath + programName, true)) == null)
                    {
                        r = key.CreateSubKey(basePath + programName);
                    }
                    r.SetValue("", programName);

                    if ((r = key.OpenSubKey(basePath + programName + "\\Shell\\Open\\Command", true)) == null)
                    {
                        r = key.CreateSubKey(basePath + programName + "\\Shell\\Open\\Command");
                    }
                    r.SetValue("", Application.ExecutablePath + " " + executionFormat);

                    if ((r = key.OpenSubKey(basePath + extension, true)) == null)
                    {
                        r = key.CreateSubKey(basePath + extension);
                    }
                    else
                    {
                        if (allowRollback)
                        {
                            var previous = r.GetValue("").ToString();

                            if (previous != programName)
                            {
                                // do rollback possibility
                                r.SetValue("Previous", r.GetValue(""));
                            }
                        }
                        r.SetValue("", programName);
                    }

                    r.Close();
                }
                key.Close();

                try
                {
                    // delete explorer info HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.{EXT}
                    key = Registry.CurrentUser;
                    key.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + extension);
                }
                catch (Exception) { }

                NativeMethods.SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        public static bool RollbackExtension(bool isGlobal, string extension)
        {
            var key = isGlobal ? Registry.ClassesRoot : Registry.CurrentUser;
            string basePath = isGlobal ? "" : "SOFTWARE\\Classes\\";


            try
            {
                RegistryKey k;

                if ((k = key.OpenSubKey(basePath + extension, true)) != null)
                {
                    var oldValue = k.GetValue("Previous");

                    if (oldValue != null)
                    {
                        k.SetValue("", oldValue.ToString());
                        k.DeleteValue("Previous");
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }

            return false;

        }
    }
}

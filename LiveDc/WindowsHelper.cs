﻿using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LiveDc
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
    }
}
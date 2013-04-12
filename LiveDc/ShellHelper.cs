using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiveDc
{
    public static class ShellHelper
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static void FindFileInExplorer(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            // combine the arguments together
            // it doesn't matter if there is a space after ','
            string argument = @"/select, " + filePath;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        public static void Start(string command)
        {
            try
            {
                Process.Start(command);
            }
            catch (Exception x)
            {
                logger.Error("Unable to open {0} because {1}", command, x.Message);
            }
        }
    }
}

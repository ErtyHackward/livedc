using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace InnoSetupManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("usage:");
                Console.WriteLine("   InnoSetupManager.exe <path_to_iss_directory> <path_to_assembly> [version_parts=3]");
            }

            int parts = args.Length == 3 ? int.Parse(args[2]) : 3;

            var issDirPath = args[0];
            var assemblyPath = args[1];

            var version = Assembly.LoadFile(assemblyPath).GetName().Version.ToString(parts);

            var issCompilerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Inno Setup 5\\compil32.exe");

            foreach (var issFilePath in Directory.GetFiles(issDirPath, "*.iss"))
            {
                UpdateVersion(issFilePath, version);
                Process.Start(issCompilerPath, "/cc " + issFilePath).WaitForExit();
            }
        }

        static void UpdateVersion(string issFilePath, string version)
        {
            var lines = File.ReadAllLines(issFilePath);

            var prevVersion = "";

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("VersionInfoVersion"))
                {
                    prevVersion = lines[i].Substring(lines[i].IndexOf('=') + 1).Trim();
                    lines[i] = "VersionInfoVersion=" + version;
                }

                if (lines[i].StartsWith("AppVerName"))
                {
                    lines[i] = lines[i].Replace(prevVersion, version);
                }
            }

            File.WriteAllLines(issFilePath, lines);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InnoSetupVersionUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("usage:");
                Console.WriteLine("   InnoSetupVersionUpdate.exe <path_to_iss> <path_to_assembly> [version_parts=3]");
            }

            int parts = args.Length == 3 ? int.Parse(args[2]) : 3;

            var issFilePath = args[0];
            var assemblyPath = args[1];

            var version = Assembly.LoadFile(assemblyPath).GetName().Version.ToString(parts);

            var lines = File.ReadAllLines(issFilePath);

            var prevVersion = "";

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("VersionInfoVersion"))
                {
                    prevVersion = lines[i].Substring(lines[i].IndexOf('=')).Trim();
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

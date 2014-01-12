using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace LiveDc.Helpers
{
    public static class FlyLinkHelper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static string FlyDbPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "FlylinkDC++", "FlylinkDC.sqlite");
            }
        }

        public static bool IsInstalled 
        {
            get
            {
                return File.Exists(FlyDbPath);
            }
        }

        public static List<string> ReadHubs()
        {
            var hubs = new List<string>();

            try
            {

                using (
                    var conn =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;Read Only=True;", FlyDbPath)))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT val_str FROM fly_registry";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var spl = reader.GetString(0).Split('\n');
                                hubs.Add(spl[3]);
                            }
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Logger.Error("Unable to read flylink hubs {0}", x);
            }

            return hubs;
        }

    }
}

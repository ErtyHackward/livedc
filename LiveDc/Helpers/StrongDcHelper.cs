using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace LiveDc.Helpers
{
    public static class StrongDcHelper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool IsInstalled 
        {
            get { return File.Exists(ConfigPath); }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StrongDC++", "Favorites.xml"); }
        }

        public static List<string> ReadHubs()
        {
            var list = new List<string>();
            if (!IsInstalled)
            {
                return list;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(ConfigPath));

                var nodes = doc.GetElementsByTagName("Hub");

                list.AddRange(from XmlNode node in nodes select node.Attributes["Server"].InnerXml);
            }
            catch (Exception x)
            {
                Logger.Error("Unable to read StrongDC++ hubs {0}", x);
            }
            return list;
        }
    }
}

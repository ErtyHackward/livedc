using System;
using System.IO;
using System.Linq;

namespace LiveDc
{
    public class Settings
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        private string _fileName = "LiveDC\\livedc.ini";

        #region Settings

        public bool Autostart { get; set; }

        public string StoragePath { get; set; }

        public bool StorageAutoSelect { get; set; }

        public bool StorageAutoPrune { get; set; }

        public bool IdleEconomy { get; set; }

        public bool AutoUpdate { get; set; }

        public string Hubs { get; set; }

        public bool ActiveMode { get; set; }

        public string IPAddress { get; set; }

        public string Nickname { get; set; }

        #endregion

        public string SettingsFilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _fileName); }
        }
        
        public Settings()
        {
            StorageAutoSelect = true;
            StorageAutoPrune = true;
            AutoUpdate = true;
            ActiveMode = true;
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var sw = new StreamWriter(File.OpenWrite(SettingsFilePath)))
                {
                    foreach (var prop in GetType().GetProperties())
                    {
                        if (prop.CanWrite)
                            sw.WriteLine("{0}={1}", prop.Name, prop.GetValue(this, null));
                    }
                }
                logger.Info("Settings saved");
            }
            catch (Exception x)
            {
                logger.Error("Unable to write settings: {0}", x);
            }
        }

        public void Load()
        {
            try
            {
                using (var sr = new StreamReader(File.OpenRead(SettingsFilePath)))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.Contains('='))
                        {
                            logger.Warn("Invalid config line: {0}", line);
                            continue;
                        }

                        var ind = line.IndexOf('=');

                        var settingName = line.Substring(0, ind);
                        var settingValue = line.Substring(ind + 1);

                        var prop = GetType().GetProperty(settingName);

                        if (prop == null)
                        {
                            logger.Warn("Unknown setting {0}", settingName);
                            continue;
                        }

                        if (prop.PropertyType == typeof (string))
                        {
                            prop.SetValue(this, settingValue, null);
                            continue;
                        }

                        if (prop.PropertyType == typeof (int))
                        {
                            prop.SetValue(this, int.Parse(settingValue), null);
                            continue;
                        }

                        if (prop.PropertyType == typeof (bool))
                        {
                            prop.SetValue(this, bool.Parse(settingValue), null);
                            continue;
                        }

                        logger.Warn("Type {0} of setting {1} is not supported", prop.PropertyType, prop.Name);

                    }
                }
                logger.Info("Settings read success");
            }
            catch (Exception x)
            {
                logger.Error("Failed to read settings {0}", x);
            }
        }
    }
}

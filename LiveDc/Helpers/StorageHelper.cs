using System;
using System.IO;
using System.Linq;

namespace LiveDc.Helpers
{
    public static class StorageHelper
    {
        public static DriveInfo FindBestDrive()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).ToList();

            drives.Sort((d1, d2) => ( d2.AvailableFreeSpace.CompareTo(d2.AvailableFreeSpace) ));

            return drives[0];
        }

        public static string GetBestSaveDirectory()
        {
            var drive = FindBestDrive();

            if (drive.RootDirectory.Name == "C:\\")
            {
                var userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                                 "Downloads");

                if (userDownloads.StartsWith("C:\\"))
                    return userDownloads;
            }

            return Path.Combine(drive.RootDirectory.Name, "LiveDcCache");
        }
    }
}

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

            drives.Sort((d1, d2) => ( d2.AvailableFreeSpace.CompareTo(d1.AvailableFreeSpace) ));

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

        public static char GetFreeDrive(char startLetter = 'c')
        {
            for (char c = startLetter; c <= 'z'; c++)
            {
                if (IsDriveFree(c))
                    return c;
            }
            throw new ApplicationException("No free letter found to mount virtual drive");
        }

        public static bool IsDriveFree(char letter)
        {
            return !DriveInfo.GetDrives().Any(d => d.Name.Equals(letter + ":\\", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiveDc.Managers
{
    public class StorageManager
    {
        private readonly string _ownDrive;
        private readonly List<string> _storageRoots = new List<string>();

        public List<string> StorageRoots { get { return _storageRoots; } }

        public StorageManager(string ownDrive)
        {
            _ownDrive = ownDrive;
        }

        private bool HasWriteAccessToFolder(string folderPath)
        {
            try
            {
                // Attempt to get a list of security permissions from the folder. 
                // This will raise an exception if the path is read only or do not have access to view the permissions. 
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(folderPath);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public void Initialize()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.RootDirectory.Name != _ownDrive && d.IsReady && d.DriveType == DriveType.Fixed).ToList();

            foreach (var driveInfo in drives)
            {
                string path = null;

                if (driveInfo.RootDirectory.Name == "C:\\")
                {
                    var userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiveDcCache");

                    if (userDownloads.StartsWith("C:\\"))
                        path = userDownloads;
                }
                
                if (path == null)
                    path = Path.Combine(driveInfo.RootDirectory.Name, "LiveDcCache");

                _storageRoots.Add(path);
            }

            // check each path for write permission
            _storageRoots.RemoveAll(s => !HasWriteAccessToFolder(Path.GetDirectoryName(s)));
        }

        public string GetStoragePoint(long bytes)
        {
            return _storageRoots.OrderByDescending(r => new DriveInfo(Path.GetPathRoot(r)).AvailableFreeSpace - bytes).FirstOrDefault();
        }
    }
}

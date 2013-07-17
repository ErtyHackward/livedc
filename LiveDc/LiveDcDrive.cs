using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dokan;
using LiveDc.Managers;
using LiveDc.Providers;
using SharpDc.Structs;

namespace LiveDc
{
    /// <summary>
    /// Represents virtual drive
    /// </summary>
    public class LiveDcDrive : DokanOperations
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private int _count = 1;
        private char _driveLetter;

        private IEnumerable<IP2PProvider> _providers;

        private Dictionary<string, Stream> _openedFiles = new Dictionary<string, Stream>();

        public string DriveRoot
        {
            get { return _driveLetter.ToString() + ":\\"; }
        }

        /// <summary>
        /// Indicates if the virtual drive is mounted and ready to work
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Groups shared files and currently downloading ones
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Magnet> AllMagnets()
        {
            return _providers.SelectMany(p2PProvider => p2PProvider.AllMagnets());
        }

        public bool HaveFile(string fileName)
        {
            return false;
        }

        public LiveDcDrive(IEnumerable<IP2PProvider> providers)
        {
            if (providers == null)
                throw new ArgumentNullException("providers");
            _providers = providers;
        }
        
        public void Unmount()
        {
            DokanNet.DokanUnmount(_driveLetter);
            IsReady = false;
        }

        public static void Unmount(char driveLetter)
        {
            DokanNet.DokanUnmount(driveLetter);
        }

        public void MountAsync(char driveLetter, AsyncCallback callback = null)
        {
            new ThreadStart(() => Mount(driveLetter)).BeginInvoke(callback, null);
        }

        public bool Mount(char driveLetter = 'n')
        {
            if (IsReady)
                throw new InvalidOperationException("Virtual drive is alredy mounted, dismount previous one");

            if (DriveInfo.GetDrives().Any(di => di.Name.Equals(driveLetter.ToString() + ":\\", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            _driveLetter = driveLetter;

            var opt = new DokanOptions();
            opt.VolumeLabel = "LiveDC";
            opt.DebugMode = false;
            opt.MountPoint = driveLetter + ":\\";
            opt.ThreadCount = 1;

            bool success = false;
            
            try
            {
                IsReady = true;
                logger.Info("Mount virtual drive at {0}", opt.MountPoint);
                var status = DokanNet.DokanMain(opt, this);
                success = status == DokanNet.DOKAN_SUCCESS;
                if (!success)
                {
                    logger.Warn("Dokan mount status: {0} ", status);
                }
            }
            catch (Exception x)
            {
                logger.Error("Error while mouting virtual drive: {0} ", x.Message);
            }
            IsReady = false;
            
            return success;
        }

        public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
        {
            info.Context = _count++;

            //Trace.Write("Create file " + filename);

            var pureFileName = filename.Trim('\\');

            if (!string.IsNullOrEmpty(pureFileName) && AllMagnets().Any(m => m.FileName == pureFileName))
                return 0;
            
            if (filename == "\\")
            {
                info.IsDirectory = true;
                //Trace.WriteLine(" ok dir");
                return 0;
            }

            //Trace.WriteLine(" not found");
            return -DokanNet.ERROR_FILE_NOT_FOUND;
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            info.Context = _count++;
            //Trace.WriteLine("OpenDirectory: " + filename);

            if (filename == "\\")
                return 0;
            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            //Trace.WriteLine("Cleanup " + filename);
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            lock (_openedFiles)
            {
                Stream stream;

                if (_openedFiles.TryGetValue(filename, out stream))
                {
                    stream.Dispose();
                    _openedFiles.Remove(filename);
                }
            }

            //Trace.WriteLine("Closefile " + filename);
            return 0;
        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            Stream stream;
            lock (_openedFiles)
            {  
                if (!_openedFiles.TryGetValue(filename, out stream))
                {
                    var pureFileName = Path.GetFileName(filename);

                    var magnet = AllMagnets().FirstOrDefault(m => m.FileName == pureFileName);

                    stream = _providers.Select(p => p.GetStream(magnet)).First(s => s != null);

                    if (stream != null)
                        _openedFiles.Add(filename, stream);
                    else
                    {
                        logger.Error("Unable to create stream from {0}", magnet.ToString());
                    }
                }
            }

            if (stream == null)
            {
                //Trace.WriteLine("Stream does not found " + filename);
                return -1;
            }

            //Trace.WriteLine(string.Format("Reading {0} {1}", filename, offset));
            
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)stream.Read(buffer, 0, buffer.Length);
            }
            catch
            {
                return -1;
            }
            return 0;
        }

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            return -1;
        }

        public int FlushFileBuffers(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
        {
            //Trace.WriteLine("Get file info " + filename);

            // base folder
            if (filename == "\\")
            {
                fileinfo.Attributes = FileAttributes.Directory;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.Length = 0;
                return 0;
            }

            var pureFileName = filename.Trim('\\');

            var item = AllMagnets().FirstOrDefault(m => m.FileName == pureFileName);

            if (!string.IsNullOrEmpty(item.FileName))
            {
                fileinfo.Attributes = FileAttributes.ReadOnly;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.Length = item.Size;
                return 0;
            }

            // file not found
            return -1;
        }

        public int FindFiles(string filename, System.Collections.ArrayList files, DokanFileInfo info)
        {
            //Trace.WriteLine("Find files in " + filename);

            foreach (var item in AllMagnets())
            {
                var fi = new FileInformation();
                fi.Attributes = FileAttributes.ReadOnly;
                fi.CreationTime = DateTime.Now;
                fi.LastAccessTime = DateTime.Now;
                fi.LastWriteTime = DateTime.Now;
                fi.Length = item.Size;
                fi.FileName = item.FileName;
                files.Add(fi);
            }

            return 0;
        }

        public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
        {
            return -1;
        }

        public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            return -1;
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
        {
            return -1;
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int LockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            freeBytesAvailable = 10ul * 1024 * 1024 * 1024;
            totalBytes = 20ul * 1024 * 1024 * 1024;
            totalFreeBytes = 10ul * 1024 * 1024 * 1024;
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            return 0;
        }
    }

    public class LiveFolder
    {
        public LiveFolder()
        {
            Folders = new List<LiveFolder>();
            Files = new List<LiveFile>();
        }

        public string Name { get; set; }

        public List<LiveFolder> Folders { get; private set; }

        public List<LiveFile> Files { get; private set; }
    }

    public class LiveFile
    {
        public string Name { get; set; }
        public long Size { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime LastAccess { get; set; }

        public string SystemPath { get; set; }

        public DownloadItem DownloadItem { get; set; }
    }
}

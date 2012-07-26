using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Dokan;

namespace LiveDc
{
    public class LiveDcDrive : DokanOperations
    {
        private int _count = 1;

        public void MountAsync()
        {
            new ThreadStart(() => Mount()).BeginInvoke(null, null);
        }

        public bool Mount(string driveLetter = "n:\\")
        {
            if (DriveInfo.GetDrives().Any(di => di.Name.Equals(driveLetter, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            var opt = new DokanOptions();
            opt.DebugMode = true;
            opt.MountPoint = driveLetter;
            opt.ThreadCount = 1;
            var result = DokanNet.DokanMain(opt, this);
            return result == DokanNet.DOKAN_SUCCESS;
        }

        public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
        {
            info.Context = _count++;

            Trace.Write("Create file " + filename);

            if (filename == "\\myvirtualfile.txt")
            {
                Trace.WriteLine(" ok");
                return 0;
            }

            if (filename == "\\")
            {
                info.IsDirectory = true;
                Trace.WriteLine(" ok dir");
                return 0;
            }

            Trace.WriteLine(" not found");
            return -DokanNet.ERROR_FILE_NOT_FOUND;
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            info.Context = _count++;
            Trace.WriteLine("OpenDirectory: " + filename);

            if (filename == "\\")
                return 0;
            return -DokanNet.ERROR_PATH_NOT_FOUND; ;
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            Trace.WriteLine("Cleanup " + filename);
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            Trace.WriteLine("Closefile " + filename);
            return 0;
        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            var length = Math.Min(buffer.Length, 1024 * 1024 - offset);

            readBytes = (uint)length;

            if (offset == 0)
            {
                var data = Encoding.Default.GetBytes("ErtyHackward");
                Array.Copy(data, buffer, data.Length);
            }

            Trace.WriteLine("Read file " + filename);

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
            Trace.WriteLine("Get file info " + filename);

            if (filename == "\\myvirtualfile.txt")
            {

                fileinfo.Attributes = FileAttributes.ReadOnly;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.Length = 1024*1024;

                return 0;
            }

            if (filename == "\\")
            {
                fileinfo.Attributes = FileAttributes.Directory;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.Length = 0;// f.Length;
                return 0;
            }

            return -1;

            //DirectoryInfo f = new DirectoryInfo(path);

            //fileinfo.Attributes = f.Attributes;
            //fileinfo.CreationTime = f.CreationTime;
            //fileinfo.LastAccessTime = f.LastAccessTime;
            //fileinfo.LastWriteTime = f.LastWriteTime;
            //fileinfo.Length = 0;// f.Length;
            //return 0;
            
        }

        public int FindFiles(string filename, System.Collections.ArrayList files, DokanFileInfo info)
        {
            Trace.WriteLine("Find files in " + filename);

            var fi = new FileInformation();
            fi.Attributes = FileAttributes.ReadOnly ;
            fi.CreationTime = DateTime.Now;
            fi.LastAccessTime = DateTime.Now;
            fi.LastWriteTime = DateTime.Now;
            fi.Length = 1024 * 1024;
            fi.FileName = "myvirtualfile.txt";
            files.Add(fi);

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
}

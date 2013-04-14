using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace LiveDc.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public class NativeImageListEventArgs : EventArgs
    {
        public string Extension { get; set; }
        public Image Icon { get; set; }
    }

    static class NativeImageList
    {
        #region Win32Constants

        private const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        private const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_SHELLICONSIZE = 0x4;
        private const uint SHGFI_SYSICONINDEX = 0x4000;
        private const uint SHGFI_ICON = 0x100;

        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private const uint LVM_FIRST = 0x1000;
        private const uint LVM_SETIMAGELIST = LVM_FIRST + 3;
        private const uint LVSIL_NORMAL = 0;
        private const uint LVSIL_SMALL = 1;

        private const uint TVM_SETIMAGELIST = 0x1109;
        private const uint TVSIL_NORMAL = 0;
        private const uint TVSIL_STATE = 2;

        #endregion


        private static readonly int _directoryIconIndex;
        private static readonly IntPtr _largeImageList;
        private static readonly IntPtr _smallImageList;
        private static readonly Hashtable _imageIndexCache = new Hashtable();
        private static readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private static readonly Dictionary<string, Image> _bigImageCache = new Dictionary<string, Image>();
        private static readonly List<string> _pendingItems = new List<string>();

        public static event EventHandler<NativeImageListEventArgs> LargeExtensionImageLoaded;

        private static void OnLargeExtensionImageLoaded(NativeImageListEventArgs e)
        {
            var handler = LargeExtensionImageLoaded;
            if (handler != null) handler(null, e);
        }

        static NativeImageList()
        {
            _directoryIconIndex = FileIconIndex("a", true);
            SHFILEINFO fileInfo = new SHFILEINFO();

            _largeImageList = SHGetFileInfo("", 0, ref fileInfo, Marshal.SizeOf(fileInfo),
                SHGFI_SHELLICONSIZE | SHGFI_SYSICONINDEX | SHGFI_LARGEICON);

            _smallImageList = SHGetFileInfo("", 0, ref fileInfo, Marshal.SizeOf(fileInfo),
                SHGFI_SHELLICONSIZE | SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
        }


        #region DLLImport
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("Shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbfileInfo, uint uFlags);

        [DllImport("User32.DLL")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        #endregion DLLImport

        public static void SetListViewIconIndex(IntPtr controlHandle)
        {
            SendMessage(controlHandle, LVM_SETIMAGELIST, (IntPtr)LVSIL_NORMAL, _largeImageList);
            SendMessage(controlHandle, LVM_SETIMAGELIST, (IntPtr)LVSIL_SMALL, _smallImageList);
        }

        public static void SetTreeViewIconIndex(IntPtr controlHandle)
        {
            SendMessage(controlHandle, TVM_SETIMAGELIST, (IntPtr)TVSIL_NORMAL, _smallImageList);
            SendMessage(controlHandle, TVM_SETIMAGELIST, (IntPtr)TVSIL_STATE, _smallImageList);
        }

        public static int FileIconIndex(string fileName)
        {
            return FileIconIndex(fileName, false);
        }

        public static int DirectoryIconIndex
        {
            get { return _directoryIconIndex; }
        }

        public static Image TryGetLargeIcon(string ext)
        {
            lock (_bigImageCache)
            {
                Image img;
                if (!_bigImageCache.TryGetValue(ext, out img))
                {
                    if (_pendingItems.Contains(ext))
                        return img;

                    _pendingItems.Add(ext);
                    new ThreadStart(delegate { GetLargeFileIcon(ext); }).BeginInvoke(null, null);
                }

                return img;
            }
        }


        public static Image GetLargeFileIcon(string ext, bool isDirectory = false)
        {
            lock (_bigImageCache)
            {
                if (_bigImageCache.ContainsKey(ext))
                {
                    return _bigImageCache[ext];
                }
            }
            
            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo(ext, isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                            SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);

            Image i = Icon.FromHandle(shinfo.hIcon).ToBitmap();
            lock (_bigImageCache)
            {
                _bigImageCache.Add(ext, i);
                _pendingItems.Remove(ext);
            }

            OnLargeExtensionImageLoaded(new NativeImageListEventArgs { Extension = ext, Icon = i });

            return i;
        }

        public static Image GetFileIcon(string ext, bool isDirectory = false)
        {
            lock (_imageCache)
            {
                if (_imageCache.ContainsKey(ext))
                {
                    return _imageCache[ext];
                }

                SHFILEINFO shinfo = new SHFILEINFO();
                SHGetFileInfo(ext, isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                              SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

                Image i = Icon.FromHandle(shinfo.hIcon).ToBitmap();
                _imageCache.Add(ext, i);
                return i;
            }
        }





        private static int FileIconIndex(string fileName, bool isDirectory)
        {
            string ext = Path.GetExtension(fileName);
            lock (_imageIndexCache)
            {
                if (_imageIndexCache.ContainsKey(ext))
                {
                    return (int)_imageIndexCache[ext];
                }
                else
                {
                    SHFILEINFO fileInfo = new SHFILEINFO();
                    SHGetFileInfo(fileName, isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL,
                        ref fileInfo,
                        Marshal.SizeOf(fileInfo), SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES);

                    _imageIndexCache.Add(ext, fileInfo.iIcon);
                    return fileInfo.iIcon;
                }
            }
        }
    }
}
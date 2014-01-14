using System.IO;
using System.Windows.Forms;
using LiveDc.Helpers;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public class SimpleStartItem : IStartItem
    {
        private readonly LiveDcDrive _drive;

        public Magnet Magnet { get; private set; }
        public bool ReadyToStart { get; private set; }
        public string StatusMessage { get; private set; }
        public float Progress { get; private set; }
        public bool Closed { get; private set; }

        public SimpleStartItem(Magnet magnet, LiveDcDrive drive)
        {
            Magnet = magnet;
            _drive = drive;

            Closed = true;
            OpenFile();
        }

        public void Dispose()
        {
            
        }
        
        public void AddToQueue()
        {
            throw new System.NotImplementedException();
        }

        public void OpenFile()
        {
            ShellHelper.Start(Path.Combine(_drive.DriveRoot, Magnet.FileName));
        }
        
        public void MainThreadAction(Form active)
        {
            
        }
    }
}
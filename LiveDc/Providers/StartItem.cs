using System.Threading;
using System.Windows.Forms;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public abstract class StartItem : IStartItem
    {
        protected bool _cancel;
        protected bool _addToQueue;
        protected bool _started;
        
        public float Progress { get; protected set; }
        public Magnet Magnet { get; protected set; }
        public bool ReadyToStart { get; protected set; }
        public string StatusMessage { get; protected set; }
        public bool Closed { get; protected set; }

        public void AddToQueue()
        {
            _addToQueue = true;
        }

        public abstract void OpenFile();
        
        public virtual void Dispose()
        {
            _cancel = true;
        }

        public void StartIn5Seconds()
        {
            ReadyToStart = true;
            int timeout = 5;
            while (timeout-- > 0)
            {
                StatusMessage = "Файл доступен. Запуск через " + timeout;
                Progress = 1f;
                Thread.Sleep(1000);
                if (_started)
                    return;
            }
            OpenFile();
            Closed = true;
        }

        public bool UserWaits()
        {
            return !_cancel && !_addToQueue;
        }

        public virtual void MainThreadAction(Form active)
        {

        }

    }
}

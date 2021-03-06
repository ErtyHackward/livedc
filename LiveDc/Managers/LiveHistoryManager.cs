﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using SharpDc.Structs;

namespace LiveDc.Managers
{
    public class LiveHistoryManager
    {
        private readonly XmlSerializer _xml;
        private List<LiveHistoryItem> _historyList = new List<LiveHistoryItem>();

        private string HistoryFilePath { get { return Path.Combine(Settings.SettingsFolder, "history.xml"); } }
        
        public event EventHandler HistoryChanged;

        protected virtual void OnHistoryChanged()
        {
            var handler = HistoryChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public LiveHistoryManager()
        {
            _xml = new XmlSerializer(typeof(List<LiveHistoryItem>));
        }

        public void Save()
        {
            if (File.Exists(HistoryFilePath))
                File.Delete(HistoryFilePath);

            using (var fs = File.OpenWrite(HistoryFilePath))
            {
                _xml.Serialize(fs, _historyList);
            }
        }

        public void Load()
        {
            _historyList.Clear();
            if (!File.Exists(HistoryFilePath))
            {
                OnHistoryChanged();
                return;
            }

            using (var fs = File.OpenRead(HistoryFilePath))
            {
                _historyList = (List<LiveHistoryItem>)_xml.Deserialize(fs);
            }

            OnHistoryChanged();
        }

        public void AddItem(Magnet magnet)
        {
            DeleteItem(magnet);
            _historyList.Add(new LiveHistoryItem { CreateDate = DateTime.Now, Magnet = magnet });
            OnHistoryChanged();
        }

        public void DeleteItem(Magnet magnet)
        {
            var index = _historyList.FindIndex(i => i.Magnet.Equals(magnet));

            if (index != -1)
            {
                _historyList.RemoveAt(index);
                OnHistoryChanged();
            }
        }

        public IEnumerable<LiveHistoryItem> Items()
        {
            for (int i = _historyList.Count - 1; i >= 0; i--)
            {
                yield return _historyList[i];
            }
        }
    }

    [Serializable]
    public class LiveHistoryItem
    {
        public DateTime CreateDate { get; set; }

        public Magnet Magnet { get; set; }
    }
}

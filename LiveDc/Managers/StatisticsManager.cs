using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveDc.Managers
{
    /// <summary>
    /// Responsible to collect different data about program usage
    /// </summary>
    public class StatisticsManager
    {
        private readonly object _syncRoot = new object();


        public DateTime Started { get; set; }
        
        public Dictionary<string, string> Data { get; set; }

        public StatisticsManager()
        {
            Started = DateTime.Now;
            Data = new Dictionary<string, string>();
        }
        
        public void Increment(string key)
        {
            lock (_syncRoot)
            {
                string current;

                if (Data.TryGetValue(key, out current))
                {
                    var integer = int.Parse(current);
                    integer++;
                    Data[key] = integer.ToString();
                }
                else
                {
                    Data.Add(key, "1");
                }
            }
        }
        
        
    }
}

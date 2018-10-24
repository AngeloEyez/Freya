using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{


    /// <summary>
    /// Xmrig JSON Data Structure
    /// </summary>
    public class JSONXmrig
    {
        public class Rootobject
        {
            public string id { get; set; }
            public string worker_id { get; set; }
            public string version { get; set; }
            public string kind { get; set; }
            public string ua { get; set; }
            public Cpu cpu { get; set; }
            public string algo { get; set; }
            public bool hugepages { get; set; }
            public int donate { get; set; }
            public Hashrate hashrate { get; set; }
            public Results results { get; set; }
            public Connection connection { get; set; }
            public Health[] health { get; set; }
        }

        public class Cpu
        {
            public string brand { get; set; }
            public bool aes { get; set; }
            public bool x64 { get; set; }
            public int sockets { get; set; }
        }

        public class Hashrate
        {
            public float[] total { get; set; }
            public float highest { get; set; }
            public float[][] threads { get; set; }
        }

        public class Results
        {
            public int diff_current { get; set; }
            public int shares_good { get; set; }
            public int shares_total { get; set; }
            public int avg_time { get; set; }
            public int hashes_total { get; set; }
            public int[] best { get; set; }
            public object[] error_log { get; set; }
        }

        public class Connection
        {
            public string pool { get; set; }
            public int uptime { get; set; }
            public int ping { get; set; }
            public int failures { get; set; }
            public object[] error_log { get; set; }
        }

        public class Health
        {
            public string name { get; set; }
            public int clock { get; set; }
            public int mem_clock { get; set; }
            public int power { get; set; }
            public int temp { get; set; }
            public int fan { get; set; }
        }

    }
}

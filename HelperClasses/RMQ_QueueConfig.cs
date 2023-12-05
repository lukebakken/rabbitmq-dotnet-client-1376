using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.ClusterMgmt
{
    /// <summary>
    /// Stores enough config info to create a queue.
    /// </summary>
    public class RMQ_QueueConfig
    {
        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool AutoDelete { get; set; }

        public Dictionary<string, object> Arguments { get; set; }
    }
}

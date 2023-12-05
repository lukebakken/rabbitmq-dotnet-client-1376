using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.ClusterMgmt
{
    /// <summary>
    /// Contains enough config information for scripted exchange management by a cluster admin.
    /// </summary>
    public class ExchangeConfig
    {
        /// <summary>
        /// Assigned vhost in cluster.
        /// </summary>
        public string vhost { get; set; }
        /// <summary>
        /// Actual name of exchange in cluster.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Friendly description of exchange.
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// Exchange type: direct, topic, header, fanout
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Set if the exchange will live beyond a server restart.
        /// Defaults to false.
        /// </summary>
        public bool durable { get; set; }
    }
}

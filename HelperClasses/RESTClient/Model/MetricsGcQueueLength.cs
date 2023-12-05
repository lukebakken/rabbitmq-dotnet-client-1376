using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class MetricsGcQueueLength
    {
        public int connection_closed { get; set; }
        public int channel_closed { get; set; }
        public int consumer_deleted { get; set; }
        public int exchange_deleted { get; set; }
        public int queue_deleted { get; set; }
        public int vhost_deleted { get; set; }
        public int node_node_deleted { get; set; }
        public int channel_consumer_deleted { get; set; }
    }
}

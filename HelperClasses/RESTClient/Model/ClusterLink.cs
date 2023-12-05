using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class ClusterLink
    {
        public Stats stats { get; set; }
        public string name { get; set; }
        public string peer_addr { get; set; }
        public int peer_port { get; set; }
        public string sock_addr { get; set; }
        public int sock_port { get; set; }
        public long recv_bytes { get; set; }
        public long send_bytes { get; set; }
    }
}

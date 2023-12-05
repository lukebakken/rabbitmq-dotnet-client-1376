using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class Stats
    {
        public long send_bytes { get; set; }
        public DetailRate send_bytes_details { get; set; }
        public long recv_bytes { get; set; }
        public DetailRate recv_bytes_details { get; set; }
    }
}

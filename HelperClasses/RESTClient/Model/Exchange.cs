using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class Exchange
    {
        public bool auto_delete { get; set; }
        public bool durable { get; set; }
        public bool @internal { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string vhost { get; set; }
    }
}

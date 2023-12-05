using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class QueueBinding_AddRequest
    {
        public string queuename { get; set; }
        public string exchange { get; set; }
        public string routing_key { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    /// <summary>
    /// Used by the Get Queue list REST API call.
    /// </summary>
    public class QueueEntry
    {
        public bool auto_delete { get; set; }
        public bool durable { get; set; }
        public bool exclusive { get; set; }
        public string leader { get; set; }
        public string name { get; set; }
        public string node { get; set; }
        public string state { get; set; }
        public string type { get; set; }
        public string vhost { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class ChannelDetails
    {
        public string connection_name { get; set; }
        public string name { get; set; }
        public string node { get; set; }
        public int number { get; set; }
        public string peer_host { get; set; }
        public int peer_port { get; set; }
        public string user { get; set; }
    }
}

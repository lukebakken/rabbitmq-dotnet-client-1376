using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.Config
{
    public class RMQ_ClientConfig
    {
        public string Host { get; set; }
        public int Http_Port { get; set; }
        public int Amqp_Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

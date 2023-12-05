using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    /// <summary>
    /// Shows up in cluster overview queries.
    /// </summary>
    public class ExchangeType
    {
        public string name { get; set; }
        public string description { get; set; }
        public bool enabled { get; set; }
    }
}

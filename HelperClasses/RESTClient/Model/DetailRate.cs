using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    /// <summary>
    /// Simple class that maps message rate info in the message stats struct from a vhost info query.
    /// </summary>
    public class DetailRate
    {
        public double rate { get; set; }
    }
}

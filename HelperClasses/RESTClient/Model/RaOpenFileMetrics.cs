using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    public class RaOpenFileMetrics
    {
        public long ra_log_wal { get; set; }
        public long ra_log_segment_writer { get; set; }
    }
}

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
    public class Context
    {
        /// <summary>
        /// Present in the Nodes query.
        /// Absent in overview queries.
        /// </summary>
        public List<object> ssl_opts { get; set; }

        /// <summary>
        /// Present in the Nodes query.
        /// Absent in overview queries.
        /// </summary>
        public string node { get; set; }

        public string description { get; set; }
        public string path { get; set; }
        public string cowboy_opts { get; set; }
        public string port { get; set; }
        public string protocol { get; set; }
    }
}

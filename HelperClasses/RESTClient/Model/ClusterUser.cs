using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    /// <summary>
    /// Returned from user query.
    /// </summary>
    public class ClusterUser
    {
        public string name { get; set; }
        public string password_hash { get; set; }
        public string hashing_algorithm { get; set; }
        public List<string> tags { get; set; }
        public Limits limits { get; set; }
    }

    public class Limits
    {
    }
}

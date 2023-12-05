using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient
{
    static public class REST_ROUTES
    {
        /// <summary>
        /// REST endpoint for connections access.
        /// </summary>
        static public string CONST_RESTRoute_Connections = "/api/connections";

        /// <summary>
        /// REST endpoint for nodes property access.
        /// </summary>
        static public string CONST_RESTRoute_Nodes = "/api/nodes";

        /// <summary>
        /// REST endpoint for queues property access.
        /// </summary>
        static public string CONST_RESTRoute_GetQueueInfo = "/api/queues";

        /// <summary>
        /// REST endpoint for binding access.
        /// </summary>
        static public string CONST_RESTRoute_Bindings = "/api/bindings";

        /// <summary>
        /// REST endpoint for querying a list of vhosts.
        /// </summary>
        static public string CONST_RESTRoute_GetVHosts = "/api/vhosts";

        /// <summary>
        /// REST endpoint for querying a list of vhosts.
        /// </summary>
        static public string CONST_RESTRoute_GetOverview = "/api/overview";

        /// <summary>
        /// REST endpoint for querying the clustername.
        /// </summary>
        static public string CONST_RESTRoute_Get_ClusterName = "/api/cluster-name";

        /// <summary>
        /// REST endpoint for querying a list of cluster nodes.
        /// </summary>
        static public string CONST_RESTRoute_GetNodes = "/api/nodes";

        /// <summary>
        /// REST endpoint for querying a list of users.
        /// </summary>
        static public string CONST_RESTRoute_GetUsers = "/api/users";

        /// <summary>
        /// REST endpoint for querying a list of exchanges.
        /// </summary>
        static public string CONST_RESTRoute_Exchanges = "/api/exchanges";
    }
}

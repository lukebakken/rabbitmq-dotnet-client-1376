using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RMQ_QueueDeleteFailure_Test.Config;
using RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient;

namespace RMQ_QueueDeleteFailure_Test.ClusterMgmt
{
    /// <summary>
    /// Simple class that performs any setup required of persistent queues, exchanges, and users in an RMQ cluster.
    /// </summary>
    public class RMQCluster_Admin
    {
        #region Private Fields

        static protected int _instance_counter;
        static protected string _classname;

        private RMQ_RESTClient _client;

        private RMQ_ClientConfig _config;

        #endregion


        #region Public Properties

        /// <summary>
        /// If set to localhost, default credentials can be used... guest:guest.
        /// Otherwise, username and password need to be set.
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Default Http port is 15672, unless set otherwise.
        /// </summary>
        public int Http_Port { get; set; }

        /// <summary>
        /// A username and password are needed if the hostname is not localhost.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// A username and password are needed if the hostname is not localhost.
        /// </summary>
        public string Password { get; set; }

        #endregion


        #region ctor / dtor

        static RMQCluster_Admin()
        {
            _classname = nameof(RMQCluster_Admin);
        }

        public RMQCluster_Admin(RMQ_ClientConfig config)
        {
            _classname = nameof(RMQCluster_Admin);
            _instance_counter++;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{_classname} - " +
                $"Constructor started.");

            _config = config;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{_classname} - " +
                $"Constructor ended.");
        }

        #endregion


        #region Public Methods

        #endregion


        #region Private Methods

        private int Setup_Client()
        {
            if (_client != null)
                return 1;

            try
            {
                if (_config == null)
                    return -1;

                _client = new RMQ_RESTClient();
                _client.Hostname = _config.Host;
                _client.Port = _config.Http_Port;
                _client.Username = _config.Username;
                _client.Password = _config.Password;
                _client.IsSSL = false;

                return 1;
            }
            catch (Exception e)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Client)} - " +
                    $"Exception caught while setting up REST client");

                return -2;
            }
        }

        /// <summary>
        /// Checks through the cluster for the node with the least number of assigned queues.
        /// NOTE: This is a simplistict approach, as it doesn't weight queues based on actual usage.
        /// </summary>
        /// <param name="rc"></param>
        /// <returns></returns>
        static public string Determine_LeastLoadedNode(RMQ_RESTClient rc)
        {
            try
            {
                // Get the list of available nodes...
                var res = rc.Get_Nodes_Info().GetAwaiter().GetResult();
                if (res.res != 1)
                {
                    // Failed to get nodes.
                    return "";
                }
                if (res.data == null)
                {
                    // Failed to get nodes.
                    return "";
                }
                if (res.data.Length == 0)
                {
                    // Failed to get nodes.
                    return "";
                }

                // Start a list of counts...
                var counts = new Dictionary<string, int>();
                foreach (var n in res.data)
                    counts.Add(n.name, 0);

                // Get a list of queues that exist...
                var res2 = rc.Get_Queues().GetAwaiter().GetResult();
                if (res2.res != 1)
                {
                    // Failed to get queues.
                    return "";
                }
                if (res2.data == null)
                {
                    // Failed to get queues.
                    return "";
                }
                if (res2.data.Length == 0)
                {
                    // Failed to get queues.
                    return "";
                }

                // Count up queues in each node...
                foreach (var q in res2.data)
                {
                    try
                    {
                        counts[q.node]++;
                    }
                    catch (Exception) { }
                }

                // And, determine the least loaded node...
                var entry = counts.OrderBy(m => m.Value).First().Key;

                return entry;
            }
            catch (Exception e)
            {
                return "";
            }
        }

        #endregion
    }
}

using NETCore_Common.WebService;
using Newtonsoft.Json.Linq;
using RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient
{
    /* RabbitMQ REST Client NOTES:
         Here's a good reference for the REST API of RabbitMQ: https://rawcdn.githack.com/rabbitmq/rabbitmq-management/v3.8.3/priv/www/api/index.html
            https://funprojects.blog/2019/11/08/rabbitmq-rest-api/
    
        NOTE: For a VHost that is still set to default "?", this is represented in a URL as: "%2f"
                For example: http://localhost:15672/api/queues/%2f/queuename
     */

    /// <summary>
    /// This REST client provides access to RMQ services functions and properties that are not easily accessible through the RabbitMQ.Client library.
    /// It is used exclusively by the RMQ_Client class, so no standalone usage is required.
    /// NOTE: Some of the comments in this class are overly verbose; they were added to get past the IDE warnings for uncommented class elements.
    /// </summary>
    public class RMQ_RESTClient
    {
        #region Public Properties

        /// <summary>
        /// Hostname of the RMQ service.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Admin port for the RMQ service.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Virtual host path on the RMQ service.
        /// </summary>
        public string VHost { get; set; }

        /// <summary>
        /// Username for accessing the RMQ service.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// User password for accessing the RMQ service.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Set if the service is using SSL.
        /// </summary>
        public bool IsSSL { get; set; }

        /// <summary>
        /// Set if the service is using SSL.
        /// </summary>
        public bool Ignore_SSLCertErrors { get; set; }

        #endregion


        #region ctor / dtor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RMQ_RESTClient()
        {
            Hostname = "localhost";
            Port = 15672;

            VHost = "/";

            Username = "guest";
            Password = "guest";
        }

        #endregion


        #region Public Method Calls

        /// <summary>
        /// Query the cluster name.
        /// </summary>
        /// <returns></returns>
        public Task<(int res, string data)> Get_ClusterName()
        {
            try
            {
                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_Get_ClusterName}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(), Username, Password, eHttp_Verbs.GET);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Query returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    var r0 = (-1, "");
                    return Task.FromResult(r0);
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Query returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    var r1 = (-2, "");
                    return Task.FromResult(r1);
                }

                // Deserialize the name...
                JObject val = JObject.Parse(wsr.JSONResponse);
                var name = val["name"].ToString();
                var r2 = (1, name);
                return Task.FromResult(r2);
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while querying cluster name.");
                var r3 = (-2, "");
                return Task.FromResult(r3);
            }
        }


        /// <summary>
        /// Query nodes list from cluster.
        /// Returns an array-list of nodes their high-level info.
        /// </summary>
        /// <returns></returns>
        public Task<(int res, Cluster_Node_QueryResult[] data)> Get_Nodes_Info()
        {
            try
            {
                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_GetNodes}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(), Username, Password, eHttp_Verbs.GET);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Nodes query returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    var r0 = (-1, new Cluster_Node_QueryResult[0]);
                    return Task.FromResult(r0);
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Nodes query returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    var r1 = (-2, new Cluster_Node_QueryResult[0]);
                    return Task.FromResult(r1);
                }
                // Check that we got data...
                if (string.IsNullOrEmpty(wsr.JSONResponse))
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Nodes query returned nothing from RMQ cluster.");

                    var r2 = (-1, new Cluster_Node_QueryResult[0]);
                    return Task.FromResult(r2);
                }

                var d = Newtonsoft.Json.JsonConvert.DeserializeObject<Cluster_Node_QueryResult[]>(wsr.JSONResponse);
                if (d == null)
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Nodes query returned nothing from RMQ cluster.");

                    var r3 = (-1, new Cluster_Node_QueryResult[0]);
                    return Task.FromResult(r3);
                }

                var r4 = (1, d);
                return Task.FromResult(r4);
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while querying Nodes on RMQ cluster.");

                var r5 = (-2, new Cluster_Node_QueryResult[0]);
                return Task.FromResult(r5);
            }
        }


        /// <summary>
        /// Get data for a specific cluster user.
        /// Returns data for the cluster users.
        /// Returns 1 if found, 0 if not found, negatives for errors.
        /// </summary>
        /// <returns></returns>
        public Task<(int res, ClusterUser data)> Get_User(string username)
        {
            try
            {
                string uqueue = HttpUtility.UrlEncode(username);

                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_GetUsers}/{uqueue}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(), Username, Password, eHttp_Verbs.GET);
                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Users query returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    var r0 = (-1, (ClusterUser)null);
                    return Task.FromResult(r0);
                }

                if (wsr.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"User was not found.");

                    var r1 = (0, (ClusterUser)null);
                    return Task.FromResult(r1);
                }

                if (wsr.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Users query returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    var r2 = (-2, (ClusterUser)null);
                    return Task.FromResult(r2);
                }

                // Check that we got data...
                if (string.IsNullOrEmpty(wsr.JSONResponse))
                {
                    // Empty result.
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Users query returned nothing from RMQ cluster.");

                    var r3 = (-1, (ClusterUser)null);
                    return Task.FromResult(r3);
                }

                var d = Newtonsoft.Json.JsonConvert.DeserializeObject<ClusterUser>(wsr.JSONResponse);
                if (d == null)
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Users query returned nothing from RMQ cluster.");
                    var r4 = (-1, (ClusterUser)null);
                    return Task.FromResult(r4);
                }

                var r5 = (-1, d);
                return Task.FromResult(r5);
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while querying Users on RMQ cluster.");

                var r6 = (-2, (ClusterUser)null);
                return Task.FromResult(r6);
            }
        }


        /// <summary>
        /// Query queues on cluster.
        /// Returns an array-list of queues and some high-level info.
        /// Returns 1 if the list was retrieved, regardless of entry count.
        /// Return negatives for errors.
        /// </summary>
        /// <returns></returns>
        public async Task<(int res, QueueEntry[] data)> Get_Queues(string queuename = "")
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uqueue = HttpUtility.UrlEncode(queuename);

                var queuefragment = "";
                if (!string.IsNullOrEmpty(queuename))
                {
                    // The caller specified a queue name.
                    // We will filter the query for just that queue...
                    queuefragment = $"/{uvhost}/{uqueue}";
                }

                // This call will get the list of queues, with the same details as in the QueueEntry class.
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_GetQueueInfo}" + queuefragment + "?columns=name,node,auto_delete,durable,exclusive,leader,vhost,type,state");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                Username,
                                                                                Password,
                                                                                eHttp_Verbs.GET);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Queue query returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return (-1, new QueueEntry[0]);
                }

                // Check if we asked for a specific queue...
                if (!string.IsNullOrEmpty(queuename))
                {
                    // We asked for a specific queue.
                    // The call can return a Not Found for this, and we need to check for it...
                    if (wsr.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Queue name was not found.

                        return (1, new QueueEntry[0]);
                    }
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Queue query returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return (-2, new QueueEntry[0]);
                }
                // Check that we got data...
                if (string.IsNullOrEmpty(wsr.JSONResponse))
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Queue query returned nothing from RMQ cluster.");

                    return (-1, new QueueEntry[0]);
                }

                // Hydrate the results based on what we expect... single or array...
                if (!string.IsNullOrEmpty(queuename))
                {
                    // Single entry...
                    var d = Newtonsoft.Json.JsonConvert.DeserializeObject<QueueEntry>(wsr.JSONResponse);
                    if (d == null)
                    {
                        // Empty result.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            "Queue query returned nothing from RMQ cluster.");

                        return (-1, new QueueEntry[0]);
                    }

                    return (1, new QueueEntry[] { d });
                }
                else
                {
                    var d = Newtonsoft.Json.JsonConvert.DeserializeObject<QueueEntry[]>(wsr.JSONResponse);
                    if (d == null)
                    {
                        // Empty result.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            "Queue query returned nothing from RMQ cluster.");

                        return (-1, new QueueEntry[0]);
                    }

                    return (1, d);
                }
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while querying Queue on RMQ cluster.");

                return (-2, new QueueEntry[0]);
            }
        }
        /// <summary>
        /// Asks the RMQ service if a particular queue exists.
        /// Returns 1 if exists, 0 if not, and negatives for errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <returns></returns>
        public async Task<int> DoesQueueExist(string queuename)
        {
            string uvhost = "";

            if (VHost == "/")
                uvhost = "%2f";
            else
                uvhost = HttpUtility.UrlEncode(VHost);

            string uqueue = HttpUtility.UrlEncode(queuename);

            // Compose the url...
            var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_GetQueueInfo}/{uvhost}/{uqueue}");

            cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

            // Make the web call...
            cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                                       Username,
                                                                                                       Password,
                                                                                                       eHttp_Verbs.GET);

            // See if the call worked...
            if (wsr.Call_ReturnCode != 1)
            {
                // Call failed.
                return -1;
            }

            if (wsr.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // The queue was found by name.
                // We have its info, but don't need it.
                // Return that the queue was found.

                return 1;
            }
            else if (wsr.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // The queue was not found.

                return 0;
            }
            else if (wsr.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Our credentials are bad.

                return -2;
            }
            else
            {
                // Unknown error.

                return -3;
            }
        }
        /// <summary>
        /// Attempts to add a queue by name.
        /// Returns 1 if added, 0 if already exists, and negatives for errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <returns></returns>
        public async Task<int> AddQuorumQueue(string queuename, string nodename)
        {
            string uvhost = "";

            if (VHost == "/")
                uvhost = "%2f";
            else
                uvhost = HttpUtility.UrlEncode(VHost);

            string uqueue = HttpUtility.UrlEncode(queuename);

            var ar = Queue_AddRequest.Create_QuorumQueue_Config(queuename, nodename);
            var jsonstring = Newtonsoft.Json.JsonConvert.SerializeObject(ar);

            // Compose the url...
            var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_GetQueueInfo}/{uvhost}/{uqueue}");

            cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

            // Make the web call...
            cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                            Username,
                                                                            Password,
                                                                            eHttp_Verbs.PUT,
                                                                            null,
                                                                            jsonstring);

            // See if the call worked...
            if (wsr.Call_ReturnCode != 1)
            {
                // Call failed.
                return -1;
            }

            if (wsr.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Our credentials are bad.

                return -2;
            }
            if (wsr.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                // The queue already exists.

                return 0;
            }
            if (wsr.StatusCode != System.Net.HttpStatusCode.Created)
            {
                // The queue was deleted by name.
                return -1;
            }

            return 1;
        }


        /// <summary>
        /// Asks the RMQ service if a particular queue binding exists.
        /// Returns 1 if exists, 0 if not, and negatives for errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <returns></returns>
        public async Task<int> DoesBindingExist(string queuename, string exchangename, string routingkey)
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uqueue = HttpUtility.UrlEncode(queuename);

                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_GetQueueInfo}/{uvhost}/{uqueue}/bindings");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                                           Username,
                                                                                                           Password,
                                                                                                           eHttp_Verbs.GET);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Binding query returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return -1;
                }
                if (wsr.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return 0;
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Binding query returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return -2;
                }
                // Check that we got data...
                if (string.IsNullOrEmpty(wsr.JSONResponse))
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Binding query returned nothing from RMQ cluster.");

                    return -1;
                }

                var d = Newtonsoft.Json.JsonConvert.DeserializeObject<QueueBinding[]>(wsr.JSONResponse);
                if (d == null)
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Binding query returned nothing from RMQ cluster.");

                    return -1;
                }
                if (d.Length == 0)
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Binding query returned nothing from RMQ cluster.");

                    return -1;
                }

                // Check for a match (source/destination/destinationtype/routingkey/vhost(already done)...
                if (d.Any(m => m.source == exchangename &&
                               m.routing_key == routingkey &&
                               m.destination == queuename &&
                               m.destination_type == "queue"))
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while querying Binding on RMQ cluster.");

                return -2;
            }
        }
        /// <summary>
        /// Attempts to add a binding.
        /// Returns 1 if successful, 0 if the queue or exchange doesn't exist, negatives for errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <param name="exchangename"></param>
        /// <param name="routingkey"></param>
        /// <param name="argslist"></param>
        /// <returns></returns>
        public async Task<int> AddBinding(string queuename, string exchangename, string routingkey, string argslist = "")
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uexch = HttpUtility.UrlEncode(exchangename);
                string uqueue = HttpUtility.UrlEncode(queuename);

                // Compose the url...
                // /api/bindings/vhost/e/exchange/q/queue
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_Bindings}/{uvhost}/e/{uexch}/q/{uqueue}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Quick and dirty json of the binding request body...
                var jsonbody = "{\"routing_key\":\"" + routingkey + "\", \"arguments\":{" + argslist + "}}";

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                                           Username,
                                                                                                           Password,
                                                                                                           eHttp_Verbs.POST,
                                                                                                           null,
                                                                                                           jsonbody);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Binding add returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return -2;
                }
                if (wsr.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Binding add returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return 0;
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Binding add returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return -2;
                }

                return 1;
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while Adding Binding on RMQ cluster.");

                return -2;
            }
        }


        /// <summary>
        /// Asks the RMQ service if a particular exchange exists.
        /// Returns 1 if exists, 0 if not, and negatives for errors.
        /// </summary>
        /// <param name="exchangename"></param>
        /// <returns></returns>
        public async Task<int> DoesExchangeExist(string exchangename)
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uqueue = HttpUtility.UrlEncode(exchangename);

                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_Exchanges}/?columns=name,vhost,auto_delete,durable,type,internal");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                Username,
                                                                                Password,
                                                                                eHttp_Verbs.GET);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange query returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return -1;
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange query returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return -2;
                }
                // Check that we got data...
                if (string.IsNullOrEmpty(wsr.JSONResponse))
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Exchange query returned nothing from RMQ cluster.");

                    return -1;
                }

                var d = Newtonsoft.Json.JsonConvert.DeserializeObject<Exchange[]>(wsr.JSONResponse);
                if (d == null)
                {
                    // Empty result.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        "Exchange query returned nothing from RMQ cluster.");

                    return -1;
                }

                // Check if the given exchange is in the returned list...
                var res = d.Any(m => m.name == exchangename);

                if (res)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while querying Exchanges on RMQ cluster.");

                return -2;
            }
        }
        /// <summary>
        /// Adds the exchange to the cluster.
        /// Returns 1 if added, negatives for errors.
        /// </summary>
        /// <param name="exchangename"></param>
        /// <returns></returns>
        public async Task<int> AddDirectExchange(string exchangename)
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uexch = HttpUtility.UrlEncode(exchangename);

                // Quick and dirty exchange request body...
                string jsonreq = "{\"type\":\"direct\",\"auto_delete\":false,\"durable\":true,\"internal\":false,\"arguments\":{}}";

                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_Exchanges}/{uvhost}/{uexch}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                Username,
                                                                                Password,
                                                                                eHttp_Verbs.PUT,
                                                                                null,
                                                                                jsonreq);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange add returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return -1;
                }
                if (wsr.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // Already exists.
                    return 1;
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange add returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return -2;
                }

                return 1;
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while adding Exchange to RMQ cluster.");

                return -2;
            }
        }
        /// <summary>
        /// Adds the exchange to the cluster.
        /// Returns 1 if added, negatives for errors.
        /// </summary>
        /// <param name="exchangename"></param>
        /// <returns></returns>
        public async Task<int> AddTopicExchange(string exchangename)
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uexch = HttpUtility.UrlEncode(exchangename);

                // Quick and dirty exchange request body...
                string jsonreq = "{\"type\":\"topic\",\"auto_delete\":false,\"durable\":true,\"internal\":false,\"arguments\":{}}";

                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_Exchanges}/{uvhost}/{uexch}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                Username,
                                                                                Password,
                                                                                eHttp_Verbs.PUT,
                                                                                null,
                                                                                jsonreq);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange add returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return -1;
                }
                if (wsr.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // Already exists.
                    return 1;
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange add returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return -2;
                }

                return 1;
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while adding Exchange to RMQ cluster.");

                return -2;
            }
        }
        /// <summary>
        /// Adds the exchange to the cluster.
        /// Returns 1 if added, negatives for errors.
        /// </summary>
        /// <param name="exchangename"></param>
        /// <returns></returns>
        public async Task<int> AddFanoutExchange(string exchangename)
        {
            try
            {
                string uvhost = "";

                if (VHost == "/")
                    uvhost = "%2f";
                else
                    uvhost = HttpUtility.UrlEncode(VHost);

                string uexch = HttpUtility.UrlEncode(exchangename);

                // Quick and dirty exchange request body...
                string jsonreq = "{\"type\":\"fanout\",\"auto_delete\":false,\"durable\":true,\"internal\":false,\"arguments\":{}}";

                // Compose the url...
                var uri = new Uri($"{(IsSSL ? "https" : "http")}://{Hostname}:{Port}{REST_ROUTES.CONST_RESTRoute_Exchanges}/{uvhost}/{uexch}");

                cWebService_Client_v4.Ignore_SSL_Certificate_Errors = Ignore_SSLCertErrors;

                // Make the web call...
                cWebClient_Return wsr = cWebService_Client_v4.Web_Request_Method(uri.ToString(),
                                                                                Username,
                                                                                Password,
                                                                                eHttp_Verbs.PUT,
                                                                                null,
                                                                                jsonreq);

                // See if the call worked...
                if (wsr.Call_ReturnCode != 1)
                {
                    // Call failed.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange add returned error: Call_ReturnCode = ({wsr.Call_ReturnCode.ToString() ?? ""}).");

                    return -1;
                }
                if (wsr.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // Already exists.
                    return 1;
                }
                if (wsr.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"Exchange add returned: StatusCode = ({wsr.StatusCode.ToString() ?? ""}).");

                    return -2;
                }

                return 1;
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    "Exception occurred while adding Exchange to RMQ cluster.");

                return -2;
            }
        }

        #endregion
    }
}

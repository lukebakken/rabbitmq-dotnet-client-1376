using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    /// <summary>
    /// Used to add a queue via REST API.
    /// </summary>
    public class Queue_AddRequest
    {
        public bool auto_delete { get; set; }
        public bool durable { get; set; }
        public bool exclusive { get; set; }
        public QueueArguments arguments { get; set; }
        public string node { get; set; }

        static public Queue_AddRequest Create_QuorumQueue_Config(string queuename, string nodename)
        {
            var ar = new Queue_AddRequest();

            ar.node = nodename;
            ar.durable = true;
            ar.exclusive = false;
            ar.auto_delete = false;
            ar.arguments = new QueueArguments();
            ar.Set_QuorumType();

            return ar;
        }

        public void Set_QuorumType()
        {
            if(this.arguments == null)
                this.arguments = new QueueArguments();

            this.arguments.xqueuetype = "quorum";
        }
        public void Set_ClassicType()
        {
            if(this.arguments == null)
                this.arguments = new QueueArguments();

            this.arguments.xqueuetype = "classic";
        }
    }

    public class QueueArguments
    {
        [JsonProperty("x-queue-type")]
        public string xqueuetype { get; set; }
    }
}

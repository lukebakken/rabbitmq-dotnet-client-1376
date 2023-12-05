using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);

    public class Consumer
    {
        public Arguments arguments { get; set; }
        public bool ack_required { get; set; }
        public bool active { get; set; }
        public string activity_status { get; set; }
        public ChannelDetails channel_details { get; set; }
        public string consumer_tag { get; set; }
        public bool exclusive { get; set; }
        public int prefetch_count { get; set; }
        public Queue queue { get; set; }
    }

    public class Arguments
    {
    }

    public class Queue
    {
        public string name { get; set; }
        public string vhost { get; set; }
    }
}

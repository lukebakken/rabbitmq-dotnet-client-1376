using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient.Model
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Cluster_Node_QueryResult>>(myJsonResponse);

    public class Cluster_Node_QueryResult
    {
        public List<object> partitions { get; set; }
        public string os_pid { get; set; }
        public long fd_total { get; set; }
        public int sockets_total { get; set; }
        public object mem_limit { get; set; }
        public bool mem_alarm { get; set; }
        public long disk_free_limit { get; set; }
        public bool disk_free_alarm { get; set; }
        public int proc_total { get; set; }
        public string rates_mode { get; set; }
        public object uptime { get; set; }
        public int run_queue { get; set; }
        public int processors { get; set; }
        public List<ExchangeType> exchange_types { get; set; }
        public List<AuthMechanism> auth_mechanisms { get; set; }
        public List<Application> applications { get; set; }
        public List<Context> contexts { get; set; }
        public List<string> log_files { get; set; }
        public string db_dir { get; set; }
        public List<string> config_files { get; set; }
        public long net_ticktime { get; set; }
        public List<string> enabled_plugins { get; set; }
        public string mem_calculation_strategy { get; set; }
        public RaOpenFileMetrics ra_open_file_metrics { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public bool running { get; set; }
        public int mem_used { get; set; }
        public DetailRate mem_used_details { get; set; }
        public int fd_used { get; set; }
        public DetailRate fd_used_details { get; set; }
        public int sockets_used { get; set; }
        public DetailRate sockets_used_details { get; set; }
        public int proc_used { get; set; }
        public DetailRate proc_used_details { get; set; }
        public object disk_free { get; set; }
        public DetailRate disk_free_details { get; set; }
        public int gc_num { get; set; }
        public DetailRate gc_num_details { get; set; }
        public object gc_bytes_reclaimed { get; set; }
        public DetailRate gc_bytes_reclaimed_details { get; set; }
        public int context_switches { get; set; }
        public DetailRate context_switches_details { get; set; }
        public long io_read_count { get; set; }
        public DetailRate io_read_count_details { get; set; }
        public long io_read_bytes { get; set; }
        public DetailRate io_read_bytes_details { get; set; }
        public double io_read_avg_time { get; set; }
        public DetailRate io_read_avg_time_details { get; set; }
        public long io_write_count { get; set; }
        public DetailRate io_write_count_details { get; set; }
        public long io_write_bytes { get; set; }
        public DetailRate io_write_bytes_details { get; set; }
        public double io_write_avg_time { get; set; }
        public DetailRate io_write_avg_time_details { get; set; }
        public long io_sync_count { get; set; }
        public DetailRate io_sync_count_details { get; set; }
        public double io_sync_avg_time { get; set; }
        public DetailRate io_sync_avg_time_details { get; set; }
        public long io_seek_count { get; set; }
        public DetailRate io_seek_count_details { get; set; }
        public double io_seek_avg_time { get; set; }
        public DetailRate io_seek_avg_time_details { get; set; }
        public long io_reopen_count { get; set; }
        public DetailRate io_reopen_count_details { get; set; }
        public long mnesia_ram_tx_count { get; set; }
        public DetailRate mnesia_ram_tx_count_details { get; set; }
        public long mnesia_disk_tx_count { get; set; }
        public DetailRate mnesia_disk_tx_count_details { get; set; }
        public long msg_store_read_count { get; set; }
        public DetailRate msg_store_read_count_details { get; set; }
        public long msg_store_write_count { get; set; }
        public DetailRate msg_store_write_count_details { get; set; }
        public long queue_index_write_count { get; set; }
        public DetailRate queue_index_write_count_details { get; set; }
        public long queue_index_read_count { get; set; }
        public DetailRate queue_index_read_count_details { get; set; }
        public long connection_created { get; set; }
        public DetailRate connection_created_details { get; set; }
        public long connection_closed { get; set; }
        public DetailRate connection_closed_details { get; set; }
        public long channel_created { get; set; }
        public DetailRate channel_created_details { get; set; }
        public long channel_closed { get; set; }
        public DetailRate channel_closed_details { get; set; }
        public long queue_declared { get; set; }
        public DetailRate queue_declared_details { get; set; }
        public long queue_created { get; set; }
        public DetailRate queue_created_details { get; set; }
        public long queue_deleted { get; set; }
        public DetailRate queue_deleted_details { get; set; }
        public List<ClusterLink> cluster_links { get; set; }
        public MetricsGcQueueLength metrics_gc_queue_length { get; set; }
    }
}

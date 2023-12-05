using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RMQ_QueueDeleteFailure_Test.ClusterMgmt;
using RMQ_QueueDeleteFailure_Test.HelperClasses.RESTClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMQ_QueueDeleteFailure_Test.ClassesUnderTest
{
    public class RMQ_Client : IDisposable
    {
        #region Private Fields

        static protected int _instance_counter;
        protected string _classname;
        protected bool disposedValue;

        protected IConnection _conn;

        protected Dictionary<string, IModel> _channels;

        #endregion


        #region Public Delegates

        public delegate void DelConnection(RMQ_Client qc);
        private DelConnection _delConnectionClosed;
        /// <summary>
        /// Called when the RMQ connection closes.
        /// This is useful, to restart the RMQ client.
        /// Assign a callback to handle this scenario.
        /// </summary>
        public DelConnection OnConnectionClosed
        {
            set
            {
                _delConnectionClosed = value;
            }
        }


        public delegate void DelChannel(RMQ_Client qc);
        private DelChannel _delChannelClosed;
        /// <summary>
        /// Called when a channel closes.
        /// Assign a callback to handle this event.
        /// </summary>
        public DelChannel OnChannelClosed
        {
            set
            {
                _delChannelClosed = value;
            }
        }


        public delegate void DelFlowControl(RMQ_Client qc, bool inflowcontrol);
        private DelFlowControl _delFlowControl;
        /// <summary>
        /// Called when flow control is enabled.
        /// This occurs when significant message backpressure has occurred, and is limiting producers from pushing messages.
        /// Assign a callback if your implementation needs to know when sending is being throttled.
        /// </summary>
        public DelFlowControl OnFlowControl
        {
            set
            {
                _delFlowControl = value;
            }
        }

        /// <summary>
        /// This delegate type is for derived clients that want to use the generic message handling method (Handle_Message_Received<TMessage>).
        /// If so, use this type as the signature for a public delegate property and private delegate field that will hold the external handler callback.
        /// NOTE: This delegate type doesn't need to appear in your derived client class.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="qc"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public delegate int DelGenericMessageReceived<TMessage>(RMQ_Client qc, TMessage msg);

        #endregion


        #region Public Properties

        ///// <summary>
        ///// Tells the connection to automatically recover lost connections.
        ///// </summary>
        //public bool AutoRecoveryEnabled { get; set; }

        /// <summary>
        /// Represents the name of the client.
        /// This should be set to the name of the service that owns the client instance, so it's connection and resources can be identified across the cluster.
        /// </summary>
        public string ClientProvidedName { get; set; }

        /// <summary>
        /// If set to localhost, default credentials can be used... guest:guest.
        /// Otherwise, username and password need to be set.
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Default Amqp port is 5672, unless set otherwise.
        /// </summary>
        public int Amqp_Port { get; set; }
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

        /// <summary>
        /// Set to the clustername the client connection belongs to.
        /// </summary>
        public string ClusterName { get; protected set; }

        public bool InError { get; protected set; }

        public bool IsReady
        {
            get
            {
                if (disposedValue)
                    return false;

                if (InError)
                    return false;

                if (_conn == null)
                    return false;

                if (!_conn.IsOpen)
                    return false;

                if (!_channels.All(m => m.Value.IsOpen))
                    return false;

                // If here, all good.
                return true;
            }
        }

        #endregion


        #region ctor / dtor

        public RMQ_Client()
        {
            _classname = nameof(RMQ_Client);

            _instance_counter++;
            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{_classname} - " +
                $"Constructor started.");

            _channels = new Dictionary<string, IModel>();

            Host = "localhost";
            Amqp_Port = 5672;
            Http_Port = 15672;

            disposedValue = false;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{_classname} - " +
                $"Constructor ended.");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)

                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RMQ_Base()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion


        #region Control Methods

        public int Start()
        {
            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                    $"Start method called on disposed instance.");

                return -1;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                $"Attempting to start client instance...");

            // Setup the connection...
            int res1 = Setup_Connection();
            if (res1 != 1)
            {
                // Failed to setup connection.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                    $"Failed to setup connection. Cannot start start client instance.");

                InError = true;

                return -1;
            }

            // Setup the queues...
            int res2 = Setup_Queues();
            if (res2 != 1)
            {
                // Failed to setup queues.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                    $"Failed to setup queues. Cannot start start client instance.");

                InError = true;

                return -1;
            }

            // Setup any consumers the instance requires...
            int res3 = SetupConsumers();
            if (res3 != 1)
            {
                // Failed to setup consumers.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                    $"Failed to setup consumers. Cannot start start client instance.");

                InError = true;

                return -1;
            }

            // Setup any exchange publishers...
            // This is called, to declare channels for any publishers that send to exchanges via routingkey, NOT queue name.
            int res4 = Setup_ExchangePublishers();
            if (res4 != 1)
            {
                // Failed to setup exchange publishers.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                    $"Failed to setup exchange publishers. Cannot start start client instance.");

                InError = true;

                return -1;
            }

            // Expose the clustername...
            var res = Get_ClusterName();
            if (res.res == 1)
                ClusterName = res.name ?? "";

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Start)} - " +
                $"Client instance started.");

            InError = false;

            return 1;
        }

        /// <summary>
        /// Override this method to release any delegates of the derived class.
        /// </summary>
        /// <returns></returns>
        public virtual int Stop()
        {
            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Attempting to stop and release client resources...");

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Closing channels...");

            // Close down all channels...
            foreach (var c in _channels)
            {
                // Skip null channels...
                if (c.Value == null)
                    continue;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                    $"Closing channel: {c.Key}...");

                try
                {
                    c.Value?.Close();
                }
                catch (Exception e) { }

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                    $"Channel: {c.Key} closed.");

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                    $"Disposing channel: {c.Key}...");

                // Impose a small delay to ensure threads are allowed to context switch and process...
                System.Threading.Thread.Sleep(1000);

                try
                {
                    c.Value?.Dispose();
                }
                catch (Exception e) { }

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                    $"Channel: {c.Key} closed.");

                // Impose a small delay to ensure threads are allowed to context switch and process...
                System.Threading.Thread.Sleep(1000);
            }
            _channels.Clear();

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Channels released. Attempting to close connection...");

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Pausing between channel shutdown and connection closure...");

            // Impose a small delay to ensure threads are allowed to context switch and process...
            System.Threading.Thread.Sleep(4000);

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Closing connection...");

            // Close the connection...
            try
            {
                _conn?.Close();
            }
            catch (Exception e) { }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Disposing connection...");

            try
            {
                _conn?.Dispose();
            }
            catch (Exception e) { }
            _conn = null;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Releasing delegates...");

            // Clear delegates...
            _delChannelClosed = null;
            _delConnectionClosed = null;
            _delFlowControl = null;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Stop)} - " +
                $"Stop method completed.");

            return 1;
        }

        #endregion


        #region Setup and Teardown

        private int Setup_Connection()
        {
            bool successful = false;

            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Connection)} - " +
                    $"Setup Connection method called on disposed instance.");

                return -1;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Connection)} - " +
                $"Attempting to create new connection...");

            try
            {
                // Get a connection factory reference...
                var factory = new ConnectionFactory()
                {
                    HostName = Host,

                    //// Enable connection recovery, for when the socket is closed or lost...
                    //AutomaticRecoveryEnabled = this.AutoRecoveryEnabled,
                    ClientProvidedName = ClientProvidedName ?? ""
                };

                // Set the username if defined...
                if (!string.IsNullOrEmpty(Username))
                    factory.UserName = Username;

                // Set the password if defined...
                if (!string.IsNullOrEmpty(Password))
                    factory.Password = Password;

                // Set the port if defined..
                factory.Port = Amqp_Port;

                // Create the client connection...
                _conn = factory.CreateConnection();

                _conn.ConnectionShutdown += Handle_Connection_Shutdown;

                successful = true;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Connection)} - " +
                    $"New connection created.");

                return 1;
            }
            catch (Exception e)
            {
                // Failed to create connection.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Connection)} - " +
                    $"Exception occurred while attempting to create new channel.");

                InError = true;

                return -2;
            }
            finally
            {
                if (!successful)
                    _conn?.Dispose();
            }
        }

        /// <summary>
        /// Creates and returns a channel.
        /// Accepts the local channel name for tracking purposes.
        /// This is usually the queue name, but for some producer types, the channelname will be the exchange name.
        /// </summary>
        /// <param name="channelname"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        protected int Create_NewChannel(string channelname, out IModel channel)
        {
            bool successful = false;

            channel = null;

            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_NewChannel)} - " +
                    $"Create New Channel method called on disposed instance.");

                channel = null;
                return -1;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_NewChannel)} - " +
                $"Attempting to create new channel ({channelname})...");

            try
            {
                // Create a channel for the queue...
                channel = _conn.CreateModel();

                channel.ModelShutdown += Handle_Channel_Shutdown;
                channel.FlowControl += Handle_Channel_FlowControlEvent;

                // Store the channel, so we can dispose it later...
                _channels.Add(channelname, channel);

                successful = true;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_NewChannel)} - " +
                    $"New channel ({channelname}) created.");

                return 1;
            }
            catch (Exception e)
            {
                // Failed to create queue on channel.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_NewChannel)} - " +
                    $"Exception occurred while attempting to create new channel ({channelname}).");

                InError = true;

                return -2;
            }
            finally
            {
                if (!successful)
                {
                    try
                    {
                        channel?.Close();
                    }
                    catch (Exception e) { }
                    try
                    {
                        channel?.Dispose();
                    }
                    catch (Exception e) { }
                    channel = null;

                    // Dispose each channel...
                    foreach (var c in _channels.Values)
                    {
                        try
                        {
                            c?.Close();
                        }
                        catch (Exception e) { }
                        try
                        {
                            c?.Dispose();
                        }
                        catch (Exception e) { }
                    }
                    _channels.Clear();
                }
            }
        }

        /// <summary>
        /// Creates and returns a temporary channel for administrative use.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        protected int Create_TemporaryChannel(out IModel channel)
        {
            bool successful = false;

            channel = null;

            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_TemporaryChannel)} - " +
                    $"Create admin Channel method called on disposed instance.");

                channel = null;
                return -1;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_TemporaryChannel)} - " +
                $"Attempting to create admin channel...");

            try
            {
                // Create a channel for the queue...
                channel = _conn.CreateModel();

                successful = true;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_TemporaryChannel)} - " +
                    $"Admin channel created.");

                return 1;
            }
            catch (Exception e)
            {
                // Failed to create queue on channel.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Create_TemporaryChannel)} - " +
                    $"Exception occurred while attempting to create admin channel.");

                InError = true;

                return -2;
            }
            finally
            {
                if (!successful)
                {
                    try
                    {
                        channel?.Close();
                    }
                    catch (Exception e) { }
                    try
                    {
                        channel?.Dispose();
                    }
                    catch (Exception e) { }
                    channel = null;
                }
            }
        }

        /// <summary>
        /// Override this method to add each consumer the client needs.
        /// </summary>
        /// <returns></returns>
        protected virtual int SetupConsumers()
        {
            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(SetupConsumers)} - " +
                    $"Setup Consumers method called on base class.");

                return -1;
            }

            return 1;
        }


        /// <summary>
        /// This method creates channels for each exchange publisher (by routingkey or topic).
        /// This is a special case of publisher that pushes messages to an exchange, via topic or routingkey, not by queue name.
        /// </summary>
        /// <returns></returns>
        protected virtual int Setup_ExchangePublishers()
        {
            bool successful = false;

            IModel channel = null;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_ExchangePublishers)} - " +
                $"Attempting to setup exchange publishers...");

            try
            {
                // Get a list of exchanges that we will publish to...
                var exchl = Get_ExchangeNames();

                // Create a channel for each exchange that we plan to publish to...
                foreach (var xch in exchl)
                {
                    // Create a channel to associate with the exchange...
                    if (Create_NewChannel(xch, out IModel ch) != 1)
                    {
                        // Failed to create the channel for the exchange.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_ExchangePublishers)} - " +
                            $"Failed to create channel.");

                        InError = true;

                        return -2;
                    }
                }

                successful = true;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_ExchangePublishers)} - " +
                    $"Exchanges are setup.");

                return 1;
            }
            catch (Exception e)
            {
                // Failed to create channel for exchange.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_ExchangePublishers)} - " +
                    $"Exception occurred while attempting create channel for exchange.");

                InError = true;

                return -2;
            }
            finally
            {
                if (!successful)
                {
                    try
                    {
                        channel?.Close();
                    }
                    catch (Exception) { }
                    try
                    {
                        channel?.Dispose();
                    }
                    catch (Exception) { }
                    channel = null;

                    // Dispose each channel...
                    foreach (var c in _channels.Values)
                    {
                        try
                        {
                            c?.Close();
                        }
                        catch (Exception) { }
                        try
                        {
                            c?.Dispose();
                        }
                        catch (Exception) { }
                    }
                    _channels.Clear();
                }
            }
        }

        /// <summary>
        /// This method will create channels for each persistent queue of the client.
        /// To make this work, add all persistent queue names to a method override of Get_QueueNames in your derived class.
        /// </summary>
        /// <returns></returns>
        protected int Setup_Queues()
        {
            bool successful = false;

            IModel channel = null;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Queues)} - " +
                $"Attempting to setup queues...");

            try
            {
                // Add any channels that we need for each publisher and consumer of the client...
                var ql = Get_QueueNames();

                // Create a channel for each queue that we plan to consume from or produce to...
                foreach (var q in ql)
                {
                    // Create a channel to associate with the queue...
                    if (Create_NewChannel(q, out IModel ch) != 1)
                    {
                        // Failed to create the channel for the queue.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Queues)} - " +
                            $"Failed to create channel.");

                        InError = true;

                        return -2;
                    }
                }

                // Add any logic needed to create non-persistent queues the client requires during its life-cycle.

                successful = true;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Queues)} - " +
                    $"Queues are setup.");

                return 1;
            }
            catch (Exception e)
            {
                // Failed to create queue on channel.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Setup_Queues)} - " +
                    $"Exception occurred while attempting create queue on channel.");

                InError = true;

                return -2;
            }
            finally
            {
                if (!successful)
                {
                    try
                    {
                        channel?.Close();
                    }
                    catch (Exception) { }
                    try
                    {
                        channel?.Dispose();
                    }
                    catch (Exception) { }
                    channel = null;

                    // Dispose each channel...
                    foreach (var c in _channels.Values)
                    {
                        try
                        {
                            c?.Close();
                        }
                        catch (Exception) { }
                        try
                        {
                            c?.Dispose();
                        }
                        catch (Exception) { }
                    }
                    _channels.Clear();
                }
            }
        }

        protected RMQ_QueueConfig Create_QueueConfig(string queuename)
        {
            // Create quorum queue with the given name...
            var q1 = new RMQ_QueueConfig();
            q1.Name = queuename;
            q1.IsDurable = true;
            q1.IsExclusive = false;
            q1.AutoDelete = false;
            q1.Arguments = new Dictionary<string, object>();
            q1.Arguments.Add("x-queue-type", "quorum");

            return q1;
        }

        /// <summary>
        /// This is a standardized method call for creating a temporary queue for a connection.
        /// It creates an exclusive queue that lives for the connection.
        /// It accepts an optional queuename, in case the client needs to generate that.
        /// Leaving the queue name blank, creates a server-named queue, and the name is in the returned queue declare struct.
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="queuename"></param>
        /// <returns></returns>
        protected virtual QueueDeclareOk Common_Create_ExclusiveQueue(IModel ch, string queuename = "")
        {
            return ch.QueueDeclare(queuename, false, true, false, null);
        }


        /// <summary>
        /// Override this method to include any exchanges that this derived client publishes to, by routingkey or topic, NOT by queuename.
        /// Contains the list of non-default exchanges this derived client pushes to.
        /// </summary>
        /// <returns></returns>
        protected virtual List<string> Get_ExchangeNames()
        {
            List<string> ql = new List<string>();

            //// Add the diagnostic test queue...
            //ql.Add(ExchangeTypes.CONST_ExchangeName_Common_Service_Command_Distribution);

            return ql;
        }
        /// <summary>
        /// Override this to add the names of persistent queues that are consumed and produced to by queue name.
        /// Returns the list of queues that are consumed or published to, via default exchange.
        /// </summary>
        /// <returns></returns>
        protected virtual List<string> Get_QueueNames()
        {
            List<string> ql = new List<string>();

            //// Add the diagnostic test queue...
            //ql.Add(QueueTypes.CONST_QueueName_Diagnostic_TestQueue);

            return ql;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Checks if a queue of the given names exists.
        /// Returns 1 if so, 0 if not, negatives for errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <returns></returns>
        public int DoesQueue_Exist(string queuename)
        {
            RMQ_RESTClient rc = new RMQ_RESTClient();
            rc.Hostname = Host;
            rc.Port = Http_Port;

            if (!string.IsNullOrEmpty(Username))
                rc.Username = Username;
            else
                rc.Username = "guest";
            if (!string.IsNullOrEmpty(Password))
                rc.Password = Password;
            else
                rc.Password = "guest";

            // Ask for the presence of the queue...
            int wres = rc.DoesQueueExist(queuename).GetAwaiter().GetResult();

            if (wres == 1)
                return 1;
            else if (wres == 0)
                return 0;
            else
                return -1;
        }
        /// <summary>
        /// This method creates a named queue, that is durable, and spans the cluster.
        /// </summary>
        /// <param name="queuename"></param>
        /// <returns></returns>
        [Obsolete("Deprecated this method call, since it tightly associates a created queue with a channel, despite not creating a consumer. Use the REST call of similar name, instead.", false)]
        public int Add_Durable_QuorumQueue(string queuename)
        {
            bool successful = false;

            IModel channel = null;

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Add_Durable_QuorumQueue)} - " +
                $"Attempting to create specific queue ({queuename})...");

            try
            {
                // Build a channel for the queue...
                var res1 = Create_NewChannel(queuename, out channel);
                if (res1 != 1)
                {
                    // Failed to create the channel.

                    return -1;
                }

                // Get the desired queue configuration...
                var qc = Create_QueueConfig(queuename);

                // Assign the queue to the channel...
                channel.QueueDeclare(queue: qc.Name,
                                     durable: qc.IsDurable,
                                     exclusive: qc.IsExclusive,
                                     autoDelete: qc.AutoDelete,
                                     arguments: qc.Arguments);

                successful = true;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Add_Durable_QuorumQueue)} - " +
                    $"Queue was setup.");

                return 1;
            }
            catch (Exception e)
            {
                // Failed to create queue on channel.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Add_Durable_QuorumQueue)} - " +
                    $"Exception occurred while attempting create queue on channel.");

                InError = true;

                return -2;
            }
            finally
            {
                if (!successful)
                {
                    // Remove the channel from the listing...
                    _channels.Remove(queuename);

                    // And, be sure to close and dispose the local reference, in case it wasn't added to the dictionary...
                    try
                    {
                        channel?.Close();
                    }
                    catch (Exception e) { }
                    try
                    {
                        channel?.Dispose();
                    }
                    catch (Exception e) { }
                    channel = null;
                }
            }
        }
        /// <summary>
        /// This method deletes a named queue.
        /// </summary>
        /// <param name="queuename"></param>
        /// <returns></returns>
        public int Delete_Queue(string queuename)
        {
            IModel ch = null;
            bool channel_needsclosing = false;

            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Delete_Queue)} - " +
                    $"Remove queue method called on disposed instance.");

                return -2;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Delete_Queue)} - " +
                $"Attempting to Remove queue...");

            try
            {
                // A queue, itself, is NOT associated with a particular channel.
                // But, the API call is a method of the channel class.
                // So, we need to grab the first channel and use it...
                if (_channels.Keys.Count == 0)
                {
                    // No available channels to use.

                    // Create an admin channel to work with...
                    if (Create_TemporaryChannel(out ch) != 1)
                    {
                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(Delete_Queue)} - " +
                            $"Failed to create admin channel.");

                        InError = true;

                        return -2;
                    }
                    // We have an admin channel to use.

                    channel_needsclosing = true;
                }
                else
                {
                    // There are channels to use.
                    if (!_channels.TryGetValue(queuename, out ch))
                    {
                        // Could not find a channel for the given queue.
                        // So, we will grab the first one, and use it.

                        // Get the first channel...
                        ch = _channels?.Values.FirstOrDefault() ?? null;
                        if (ch == null)
                        {
                            // Channel is empty.

                            OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                                $"{_classname}:{_instance_counter.ToString()}::{nameof(Delete_Queue)} - " +
                                $"Failed to get an available channel.");

                            InError = true;

                            return -2;
                        }
                    }
                    else
                    {
                        // We retrieved the channel assigned to the queue.

                        // We will set a flag, so that we know to close the channel...
                        channel_needsclosing = false;
                    }
                }
                // If here, we have a channel to use.

                // Delete the queue (if it doesn't exist)...
                ch.QueueDelete(queuename);

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Delete_Queue)} - " +
                    $"Queue deleted.");

                return 1;
            }
            catch (Exception e)
            {
                //if (e.Message.Contains("NOT_FOUND - no exchange"))
                //{
                //    // We are closing a queue, but got a missing exchange error.
                //    // This is likely because there's a queue->exchange binding that has an orphaned exchange name.
                //    // We will treat this edge-case as a valid queue deletion, because it's a flaw in the library we are using.

                //    return 1;
                //}

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Delete_Queue)} - " +
                    $"Exception occurred while attempting to delete Queue.");

                InError = true;

                return -3;
            }
            finally
            {
                if (channel_needsclosing)
                {
                    // We created an admin channel, or have a channel associated with the deleted queue.
                    // In either case, we need to close the channel.

                    // Remove the channel from the listing...
                    _channels.Remove(queuename);

                    // Close and dispose the channel...
                    try
                    {
                        ch?.Close();
                    }
                    catch (Exception e) { }
                    try
                    {
                        ch?.Dispose();
                    }
                    catch (Exception e) { }
                }
            }
        }

        /// <summary>
        /// Checks if a binding exists.
        /// Returns 1 if so, 0 if not, negatives for errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <param name="exchangename"></param>
        /// <param name="routingkey"></param>
        /// <returns></returns>
        public int DoesBinding_Exist(string queuename, string exchangename, string routingkey)
        {
            RMQ_RESTClient rc = new RMQ_RESTClient();
            rc.Hostname = Host;
            rc.Port = Http_Port;

            if (!string.IsNullOrEmpty(Username))
                rc.Username = Username;
            else
                rc.Username = "guest";
            if (!string.IsNullOrEmpty(Password))
                rc.Password = Password;
            else
                rc.Password = "guest";

            // Ask for the presence of the binding...
            int wres = rc.DoesBindingExist(queuename, exchangename, routingkey).GetAwaiter().GetResult();

            if (wres == 1)
                return 1;
            else if (wres == 0)
                return 0;
            else
                return -1;
        }
        /// <summary>
        /// Attempts to add a queue binding.
        /// Returns 1 if successful, -1 if queue not found, -2 if exchange not found, all others are errors.
        /// </summary>
        /// <param name="queuename"></param>
        /// <param name="exchangename"></param>
        /// <param name="routingkey"></param>
        /// <returns></returns>
        public int AddQueueBinding(string queuename, string exchangename, string routingkey)
        {
            IModel ch = null;
            bool channel_needsclosing = false;

            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                    $"Queue binding method called on disposed instance.");

                return -2;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                $"Attempting to add queue binding...");

            try
            {
                // A queue binding, itself, is NOT associated with a particular channel.
                // But, the API call is a method of the channel class.
                // So, we need to grab any channel and use it...
                if (_channels.Keys.Count == 0)
                {
                    // No available channels to use.

                    // Create an admin channel to work with...
                    if (Create_TemporaryChannel(out ch) != 1)
                    {
                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                            $"Failed to create admin channel.");

                        InError = true;

                        return -2;
                    }
                    // We have an admin channel to use.

                    channel_needsclosing = true;
                }
                else
                {
                    // Could not find a channel to use.
                    // So, we will grab the first one, and use it.

                    // Get the first channel...
                    ch = _channels?.Values.FirstOrDefault() ?? null;
                    if (ch == null)
                    {
                        // Channel is empty.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                            $"Failed to get an available channel.");

                        InError = true;

                        return -2;
                    }
                }
                // If here, we have a channel to use.

                // Add the binding (if it doesn't exist)...
                ch.QueueBind(queuename, exchangename, routingkey);

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                    $"Queue binding added.");

                return 1;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("NOT_FOUND - no exchange"))
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                        $"Exchange ({exchangename ?? ""}) not found while attempting to add queue binding.");

                    return -2;
                }
                if (e.Message.Contains("NOT_FOUND - no queue"))
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                        $"Queue ({queuename ?? ""}) not found while attempting to add queue binding.");

                    return -1;
                }

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(AddQueueBinding)} - " +
                    $"Exception occurred while attempting to add queue binding.");

                InError = true;

                return -3;
            }
            finally
            {
                if (channel_needsclosing)
                {
                    // We created an admin channel that we need to close.

                    // Close and dispose the channel...
                    try
                    {
                        ch?.Close();
                    }
                    catch (Exception e) { }
                    try
                    {
                        ch?.Dispose();
                    }
                    catch (Exception e) { }
                }
            }
        }


        /// <summary>
        /// Checks if an exchange exists.
        /// Returns 1 if so, 0 if not, negatives for errors.
        /// </summary>
        /// <param name="exchangename"></param>
        /// <returns></returns>
        public int DoesExchange_Exist(string exchangename)
        {
            RMQ_RESTClient rc = new RMQ_RESTClient();
            rc.Hostname = Host;
            rc.Port = Http_Port;

            if (!string.IsNullOrEmpty(Username))
                rc.Username = Username;
            else
                rc.Username = "guest";
            if (!string.IsNullOrEmpty(Password))
                rc.Password = Password;
            else
                rc.Password = "guest";

            // Ask for the presence of the exchange...
            int wres = rc.DoesExchangeExist(exchangename).GetAwaiter().GetResult();

            if (wres == 1)
                return 1;
            else if (wres == 0)
                return 0;
            else
                return -1;
        }

        /// <summary>
        /// Get the name of the connected cluster.
        /// Returns 1 if so, 0 if not, negatives for errors.
        /// </summary>
        /// <returns></returns>
        public (int res, string name) Get_ClusterName()
        {
            RMQ_RESTClient rc = new RMQ_RESTClient();
            rc.Hostname = Host;
            rc.Port = Http_Port;

            if (!string.IsNullOrEmpty(Username))
                rc.Username = Username;
            else
                rc.Username = "guest";
            if (!string.IsNullOrEmpty(Password))
                rc.Password = Password;
            else
                rc.Password = "guest";

            // Ask for the presence of the queue...
            var wres = rc.Get_ClusterName().GetAwaiter().GetResult();

            if (wres.res == 1)
                return (1, wres.data);
            else if (wres.res == 0)
                return (0, "");
            else
                return (-1, "");
        }

        #endregion


        #region Send Methods

        /// <summary>
        /// Simple call to publish a message to a queue.
        /// Derived classes can use this common publish method, to reduce the code cruft of a publish method.
        /// This call publishes the message to the default exchange, with a routingkey of the destination queue.
        ///     with the notion that all queues are registered, by RMQ, to the default exchange by their queue name as routing key.
        /// This method is chained in inbound, forwarding call-stacks (from clients), so it needs known return values, to determine if handled or not.
        /// Returns 1 if pushed to the broker, -1 for push failures or lack of connection, -2 if unroutable or empty (missing information).
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="queuename"></param>
        /// <param name="isdurable"></param>
        /// <param name="msgcategory"></param>
        /// <param name="corelationid"></param>
        /// <param name="replyto"></param>
        /// <returns></returns>
        public int Publish_Message_toQueue<T>(T msg, string queuename, bool isdurable = true, string msgcategory = null, string corelationid = null, string replyto = "")
        {
            // Publishing a message directly to a queue is really publishing the message to the default exchange, with a routing key of the queue name.
            // So, we will leverage the already built publish method, with a blank exchange name and our queuename as the routing key.
            return Publish_Message_toExchange(msg, "", queuename, isdurable, msgcategory, corelationid, replyto);
        }

        /// <summary>
        /// Simple call to publish a message to a topic, direct, or fanout exchange.
        /// Derived classes can use this common publish method, to reduce the code cruft of a publish method.
        /// This call publishes the message to the given exchange, with a routingkey/topic.
        /// NOTE: This method is also called for sending messages to a queue, because queues are bound to the default exchange by queuename as routingkey.
        /// The msgcategory is an optional parameter that is mapped to BasicProperties.Type. It can be used when the same classtype is sent for multiple message types.
        /// This method is chained in inbound, forwarding call-stacks (from clients), so it needs known return values, to determine if handled or not.
        /// Returns 1 if pushed to the broker, -1 for push failures or lack of connection, -2 if unroutable or empty (missing information).
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exchangename"></param>
        /// <param name="routingkey"></param>
        /// <param name="isdurable"></param>
        /// <param name="msgcategory"></param>
        /// <param name="corelationid"></param>
        /// <param name="replyto"></param>
        /// <returns></returns>
        public int Publish_Message_toExchange<T>(T msg, string exchangename, string routingkey, bool isdurable = true, string msgcategory = null, string corelationid = null, string replyto = "")
        {
            if (disposedValue)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                    $"Publish method called on disposed instance.");

                // Indicate a push failure...
                return -1;
            }

            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                $"Attempting to publish message.");

            // Check that either the exchange or the routingkey are set...
            // We require at least one to be defined for sending a message.
            if (string.IsNullOrEmpty(exchangename) && string.IsNullOrEmpty(routingkey))
            {
                // Exchange and routingkey are both blank.
                // Cannot send the message without a routing key to the default exchange, or a defined exchange and blank routing, or defined both.

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                    $"Given exchangename and routingkey are both blank.");

                return -2;
            }

            if (msg == null)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                    $"Given message is null, and cannot be published.");

                // Indicate a missing data error...
                return -2;
            }

            try
            {
                // Serialize the message...
                var jsonstring = JsonConvert.SerializeObject(msg);

                // Convert the message for transport...
                var body = Encoding.UTF8.GetBytes(jsonstring);

                // Determine what channel name we will call for...
                var channelname = "";
                if (string.IsNullOrEmpty(exchangename))
                {
                    // The exchange is blank.
                    // So, we are writing to the default exchange.
                    // Which means, we are likely sending to a queue that was automatically bound to the default exchange.
                    // So, we will lookup the channel by queue name... or routingkey in our case.
                    channelname = routingkey;
                }
                else
                {
                    // The exchange is defined.
                    // So, we will retrieve the channel created for publishing to the exchange.
                    channelname = exchangename;
                }

                // Get the channel assigned...
                if (!_channels.TryGetValue(channelname, out var ch))
                {
                    // Channel is not created.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                        $"Could not locate channel while attempting to publish message.");

                    // Indicate a push failure...
                    return -1;
                }

                // Create a props list that includes the class type as contenttype, so the recipient can deserialize it properly...
                IBasicProperties props = ch.CreateBasicProperties();
                props.ContentEncoding = "application/json";
                props.ContentType = OGA.SharedKernel.Serialization.Serialization_Helper.GetType_forSerialization(typeof(T));
                props.Type = msgcategory ?? "";
                props.CorrelationId = corelationid ?? "";
                props.ReplyTo = replyto ?? "";
                props.MessageId = Guid.NewGuid().ToString();

                // Set the message as needing to be persisted, if needed...
                if (isdurable)
                    props.DeliveryMode = 2;


                lock (ch)
                {
                    // Send the message to the queue...
                    ch.BasicPublish(exchange: exchangename,
                                    routingKey: routingkey,
                                    basicProperties: props,
                                    body: body);
                }

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                    $"Message published to queue.");

                // Indicate success...
                return 1;
            }
            catch (Exception e)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Publish_Message_toExchange)} - " +
                    $"Exception occurred while attempting to publish message to exchange.");

                // Indicate a general publish failure...
                return -1;
            }
        }

        #endregion


        #region Internal Event Handlers

        protected virtual void Handle_Connection_Shutdown(object sender, ShutdownEventArgs e)
        {
            OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                $"Connection shutdown event occurred.");

            try
            {
                // The connection has shutdown.

                if (OGA.SharedKernel.Logging_Base.Logger_Ref?.IsTraceEnabled ?? false)
                {
                    try
                    {
                        StringBuilder b = new StringBuilder();

                        string sendertype = sender.GetType().Name ?? "";
                        string sendertype_fullname = sender.GetType().FullName ?? "";
                        var initiator = e.Initiator.ToString() ?? "";
                        var replytext = e.ReplyText ?? "";
                        var replycode = e.ReplyCode.ToString() ?? "";

                        var cause = e.Cause?.GetType().FullName ?? "";

                        b.AppendLine($"sendertype: {sendertype}");
                        b.AppendLine($"initiator: {initiator}");
                        b.AppendLine($"replytext: {replytext}");
                        b.AppendLine($"replycode: {replycode}");
                        b.AppendLine($"cause: {cause}");
                        b.AppendLine($"sendertype_fullname: {sendertype_fullname}");

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                            $"Connection shutdown event reasons:\r\n" + b.ToString());
                    }
                    catch (Exception) { }
                }

                // Notify any connection that the connection closed...
                if (_delConnectionClosed != null)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                        $"Calling Connection Closure Delegate...");

                    _delConnectionClosed(this);

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                        $"Connection Closure Delegate returned.");
                }
                else
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                        $"No Connection Closure Delegate to call.");
                }

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                    $"Connection shutdown event handled.");
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Connection_Shutdown)} - " +
                    $"Exception occurred while processing connection shutdown event.");
            }
        }

        protected virtual void Handle_Channel_Shutdown(object sender, ShutdownEventArgs e)
        {
            try
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                    $"Channel shutdown event.");

                // The channel was shutdown.
                if (OGA.SharedKernel.Logging_Base.Logger_Ref?.IsTraceEnabled ?? false)
                {
                    try
                    {
                        StringBuilder b = new StringBuilder();

                        var initiator = e.Initiator.ToString() ?? "";
                        var replytext = e.ReplyText ?? "";
                        var replycode = e.ReplyCode.ToString() ?? "";
                        var cause = e.Cause?.GetType().FullName ?? "";

                        string sendertype = sender.GetType().Name ?? "";
                        string sendertype_fullname = sender.GetType().FullName ?? "";

                        b.AppendLine($"sendertype: {sendertype}");
                        b.AppendLine($"initiator: {initiator}");
                        b.AppendLine($"replytext: {replytext}");
                        b.AppendLine($"replycode: {replycode}");
                        b.AppendLine($"cause: {cause}");
                        b.AppendLine($"sendertype_fullname: {sendertype_fullname}");

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                            $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                            $"Channel shutdown event reasons:\r\n" + b.ToString());
                    }
                    catch (Exception) { }
                }

                // Notify any subscribers that the channel closed...
                if (_delChannelClosed != null)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                        $"Calling Channel shutdown Delegate...");

                    _delChannelClosed(this);

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                        $"Channel shutdown Delegate returned.");
                }
                else
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                        $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                        $"No Channel shutdown Delegate to call.");
                }

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Trace(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                    $"Channel shutdown event handled.");
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_Shutdown)} - " +
                    $"Exception occurred while processing channel shutdown event.");
            }
        }

        protected virtual void Handle_Channel_FlowControlEvent(object sender, FlowControlEventArgs e)
        {
            try
            {
                // Flow control state changed...
                var inflowcontrol = e.Active;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_FlowControlEvent)} - " +
                    $"Flow control is now {(inflowcontrol ? "ON" : "OFF")}.");

                // Notify any subscribers that flow control state has changed...
                if (_delFlowControl != null)
                    _delFlowControl(this, inflowcontrol);
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_Channel_FlowControlEvent)} - " +
                    $"Exception occurred while processing flow control state change event.");
            }
        }

        protected virtual void Handle_ConsumerQueue_Shutdown(object sender, ShutdownEventArgs e)
        {
            OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_ConsumerQueue_Shutdown)} - " +
                $"Consumer queue shutdown event - started.");

            try
            {
                // The queue we're consuming was shutdown.

                var initiator = e.Initiator;
                var replytext = e.ReplyText;
                var replycode = e.ReplyCode;
                var cause = e.Cause;

                OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_ConsumerQueue_Shutdown)} - " +
                    $"Consumer queue shutdown event - ended.");
            }
            catch (Exception ex)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(ex,
                    $"{_classname}:{_instance_counter.ToString()}::{nameof(Handle_ConsumerQueue_Shutdown)} - " +
                    $"Exception occurred while processing consumer queue shutdown event.");
            }
        }

        #endregion


        #region Private Method

        #endregion
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using NLog.Config;
using NLog.Targets;
using OGA.SharedKernel;
using RMQ_QueueDeleteFailure_Test.ClassesUnderTest;
using System;

namespace RMQ_QueueDeleteFailure_Test.Tests
{
    [TestClass]
    public class RMQ_Client_Tests
    {
        // This test is turned off, since there is an error in the nuget library we use with the RMQ client.
        // See this: https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/1376
        //  Test_1_4_3  Connect a queue client instance.
        //              Attempt to add a queue binding for an unknown exchange name.
        //              Verify it was not added.
        [TestMethod]
        public void Test_1_4_3()
        {
            // Send logs to the console...
            SetupLogging_toConsole();

            // Wrap testing in a try-finally to dispose the RMQ client on failure...
            RMQ_Client qc = null;
            try
            {
                // 1. Setup and start an RMQ client instance...
                {
                    /// Setup RMQ client...
                    qc = new RMQ_Client();
                    qc.Host = "localhost";
                    qc.Amqp_Port = 5672;
                    qc.Username = "guest";
                    qc.Password = "guest";
                    qc.ClientProvidedName = "RMQ_UnitTesting";

                    // Give it some bogus callbacks...
                    qc.OnChannelClosed = (qc) => { return; };
                    qc.OnConnectionClosed = (qc) => { return; };
                    qc.OnFlowControl = (qc, inflowcontrol) => { return; };

                    // Connect to the cluster...
                    int res = qc.Start();
                    if (res != 1)
                        Assert.Fail("Failed to Start");
                }

                // 2. Create a bogus queue name...
                string queuename = Guid.NewGuid().ToString();

                // 3. Create a bogus exchange name...
                string exchangename = Guid.NewGuid().ToString();

                // 4. Create a bogus routing key...
                string routingkey = Guid.NewGuid().ToString();


                // Verify the unknown exchange is truly unknown, before we continue...
                var res1a = qc.DoesExchange_Exist(exchangename);
                if (res1a != 0)
                    Assert.Fail("Exchange was present or our query failed.");


                // 5. Create a test queue...
                var res2 = qc.Add_Durable_QuorumQueue(queuename);
                if (res2 != 1)
                    Assert.Fail("Failed to add durable queue.");


                // 6. Verify the queue was created...
                var res2a = qc.DoesQueue_Exist(queuename);
                if (res2a != 1)
                    Assert.Fail("Failed to find queue.");


                // 7. Attempt to add the queue binding...
                var res3 = qc.AddQueueBinding(queuename, exchangename, routingkey);

                // 8. The above call would have returned success (1), if the queue binding was successfully added.
                // However. It properly returns an error because of the bogus exchange name we gave it... returning an error (-2)
                // The problem is...
                // Along with the returned error, the above call logic encountered a missing exception which prevented it from rolling back the pending queue binding reference in the RabbitMQ.Client library.
                // This exception is what causes the bogus exchange reference to remain dangling in the RabbitMQ.Client library that fails our later call, step #11.
                if (res3 != -2)
                    Assert.Fail("Add Queue failed to give the expected error.");


                // 9. Manual step. Not necessary, since step #10 confirms, programmatically, the binding did not actually get created.


                // 10. Verify (via REST) the queue binding failed to be created...
                // This step confirms (via REST) that the queue binding was not actually created, because the previous call was given a bogus exchange.
                var res3a = qc.DoesBinding_Exist(queuename, exchangename, routingkey);
                if (res3a != 0)
                    Assert.Fail("Expected binding to not be present.");


                // 11. Delete the queue we created already...
                // This call should return success (1) that the queue was deleted.
                // Instead, it returns an error for the occurring exception
                // However. This call is effectively broken, because of a dangling queue binding (to a missing exchange, left in step #7) that prevents this call from properly deleting the queue.
                // Stepping through this call, you will see an exception thrown for a "NOT_FOUND - no exchange".
                // Also present in the thrown exception, is the exchange name (matching the one we created above), that should NOT have remained in the RabbitMQ.Client library.
                var res4 = qc.Delete_Queue(queuename);
                if (res4 != 1)
                    Assert.Fail("Failed to delete queue.");


                // 12. Manual step to walk the "Delete_Queue" call above, to confirm the exception thrown and its message (RMQ_Client - Line 1133).


                // 13. Manual step. Not necessary, since step #14 confirms, programmatically, that the queue still exists.


                // 14. Verify (via REST) the queue still exists on the cluster.
                // We do this, to confirm the previous call (delete queue) failed to do its job, because it threw an exception for the bogus exchange reference stuck in the RabbitMQ.Client.
                var res4b = qc.DoesQueue_Exist(queuename);
                if (res4b != 0)
                    Assert.Fail("Expected queue to not be present.");

                // Disconnect the client...
                int res6 = qc.Stop();
                if (res6 != 1)
                    Assert.Fail("Failed to stop RMQ client.");
            }
            finally
            {
                if (qc != null)
                    qc?.Dispose();
            }
        }


        protected void SetupLogging_toConsole()
        {
            LoggingConfiguration loggingConfiguration = new LoggingConfiguration();
            if (loggingConfiguration.FindTargetByName("logconsole") == null)
            {
                ConsoleTarget target = new ConsoleTarget("logconsole");
                loggingConfiguration.AddRule(LogLevel.Info, LogLevel.Fatal, target);
                LogManager.Configuration = loggingConfiguration;
                LogManager.ReconfigExistingLoggers();
            }

            Logger logger2 = (Logging_Base.Logger_Ref = (Logging_Base.Logger_Ref = LogManager.GetCurrentClassLogger()));
        }
    }
}

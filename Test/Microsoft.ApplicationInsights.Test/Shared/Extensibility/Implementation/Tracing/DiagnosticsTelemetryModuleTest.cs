namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TaskEx = System.Threading.Tasks.Task;

    [TestClass]
    public class DiagnosticsTelemetryModuleTest
    {
        [TestMethod]
        public void TestModuleDefaultInitialization()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.IsTrue(string.IsNullOrEmpty(initializedModule.DiagnosticsInstrumentationKey));
                Assert.AreEqual("Error", initializedModule.Severity);

                Assert.AreEqual(1, initializedModule.Senders.Count);
                Assert.AreEqual(1, initializedModule.Senders.OfType<PortalDiagnosticsSender>().Count());
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleSetInstrumentationKey()
        {
            var diagnosticsInstrumentationKey = Guid.NewGuid().ToString();
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                initializedModule.DiagnosticsInstrumentationKey = diagnosticsInstrumentationKey;

                Assert.AreEqual(diagnosticsInstrumentationKey, initializedModule.DiagnosticsInstrumentationKey);

                Assert.AreEqual(
                    diagnosticsInstrumentationKey,
                    initializedModule.Senders.OfType<PortalDiagnosticsSender>().First().DiagnosticsInstrumentationKey);
            }
        }

        /// <summary>
        /// A replacement IDiagnosticsSender for the DiagnosticTelemetryModule to make use of upon being initialized. This 
        /// sender will check to see if the event data it is being asked to send contains the same information we set into
        /// the PreInitPortalDiagnosticsSender.
        /// </summary>
        class TestDiagnosticThrottler : IDiagnosticsSender
        {
            public int SentCount = 0;
            public int SentTestCount = 0;

            public void Send(TraceEvent eventData)
            {
                this.SentCount++;
                if (eventData.Payload.Any(a => { return ((string)a).Equals("Tester"); }))
                {
                    this.SentTestCount++;
                }
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleRetainsPreInitMessages()
        {
            var diagnosticsInstrumentationKey = Guid.NewGuid().ToString();
            var testSender = new TestDiagnosticThrottler();
            using (var initializedModule = new DiagnosticsTelemetryModule() { OverrideInitialDiagnosticSender = testSender })
            {
                var te = new TraceEvent() { MetaData = new EventMetaData() { EventId = 1, MessageFormat = "fmt", Level = EventLevel.Critical }, Payload = new object[] { "Tester" } };
                initializedModule.EventListener.WriteEvent(te);
                te = new TraceEvent() { MetaData = new EventMetaData() { EventId = 2, MessageFormat = "fmt", Level = EventLevel.Critical }, Payload = new object[] { "Tester" } };
                initializedModule.EventListener.WriteEvent(te);
                te = new TraceEvent() { MetaData = new EventMetaData() { EventId = 3, MessageFormat = "fmt", Level = EventLevel.Critical }, Payload = new object[] { "Tester" } };
                initializedModule.EventListener.WriteEvent(te);

                // grab the sender from the module and cound the number of events.
                Assert.AreEqual(3, initializedModule.Senders.OfType<PortalDiagnosticsQueueSender>().First().EventData.Count);

                initializedModule.Initialize(new TelemetryConfiguration());

                Assert.AreEqual(testSender.SentCount, testSender.SentTestCount);
                Assert.AreEqual(3, testSender.SentTestCount);
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleSetSeverity()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.AreEqual(EventLevel.Error.ToString(), initializedModule.Severity);

                initializedModule.Severity = "Informational";

                Assert.AreEqual(EventLevel.Informational, initializedModule.EventListener.LogLevel);
            }
        }

        [TestMethod]
        public void TestDiagnosticModuleDoesNotThrowIfInitailizedTwice()
        {
            using (DiagnosticsTelemetryModule module = new DiagnosticsTelemetryModule())
            {
                module.Initialize(new TelemetryConfiguration());
                module.Initialize(new TelemetryConfiguration());
            }
        }

        [TestMethod]
        public void DiagnosticModuleDoesNotThrowIfQueueSenderContinuesRecieveEvents()
        {
            using (DiagnosticsTelemetryModule module = new DiagnosticsTelemetryModule())
            {
                var queueSender = module.Senders.OfType<PortalDiagnosticsQueueSender>().First();

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var taskStarted = new AutoResetEvent(false);
                    TaskEx.Run(() =>
                    {
                        taskStarted.Set();
                        while (!cancellationTokenSource.IsCancellationRequested)
                        {
                            queueSender.Send(new TraceEvent());
                            Thread.Sleep(1);
                        }
                    }, cancellationTokenSource.Token);

                    taskStarted.WaitOne(TimeSpan.FromSeconds(5));

                    //Assert.DoesNotThrow
                    module.Initialize(new TelemetryConfiguration());

                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}

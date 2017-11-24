﻿namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationInsights.TestFramework;

    [TestClass]
    public class HealthHeartbeatTests
    {
        [TestMethod]
        public void InitializeHealthHeartbeatDoesntThrow()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatTwiceDoesntFail()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatDefaultsAreSetProperly()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                Assert.IsTrue(hbeat.DisabledDefaultFields == null || hbeat.DisabledDefaultFields.Count() == 0);
                Assert.AreEqual(hbeat.Interval, HeartbeatProvider.DefaultHeartbeatInterval);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultInterval()
        {
            TimeSpan nonDefaultInterval = TimeSpan.FromMilliseconds(10000);

            using (var hbeat = new HeartbeatProvider() { Interval = nonDefaultInterval })
            {
                hbeat.Initialize(configuration: null);
                Assert.AreEqual(nonDefaultInterval, hbeat.Interval);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithZeroIntervalRevertsToDefault()
        {
            using (var hbeat = new HeartbeatProvider() { Interval = TimeSpan.FromMilliseconds(0) })
            {
                hbeat.Initialize(configuration: null);
                Assert.AreEqual(hbeat.Interval, HeartbeatProvider.DefaultHeartbeatInterval);
            }
        }

        [TestMethod]
        public void CanExtendHeartbeatPayload()
        {

            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: new TelemetryConfiguration());

                try
                {
                    Assert.IsTrue(hbeat.AddHealthProperty("test01", "this is a value", true));
                }
                catch (Exception e)
                {
                    Assert.Fail(string.Format(CultureInfo.CurrentCulture, "Registration of a heartbeat payload provider throws exception '{0}", e.ToInvariantString()));
                }
            }
        }

        [TestMethod]
        public void CanSetDelayBetweenHeartbeats()
        {
            TimeSpan userSetInterval = TimeSpan.FromMilliseconds(7252.0);

            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);
                Assert.AreNotEqual(userSetInterval, hbeat.Interval.TotalMilliseconds);

                hbeat.Interval = userSetInterval;
                hbeat.Initialize(configuration: config);
                Assert.AreEqual(userSetInterval, hbeat.Interval);
            }
        }

        [TestMethod]
        public void DiagnosticsTelemetryModuleCreatesHeartbeatModule()
        {
            using (var diagModule = new DiagnosticsTelemetryModule())
            {
                diagModule.Initialize(new TelemetryConfiguration());
                Assert.IsNotNull(diagModule.HeartbeatProvider);
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsDataByDefault()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);
                var hbeatPayloadData = hbeat.GatherData();
                Assert.IsNotNull(hbeatPayloadData);
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                string testerKey = "tester123";
                Assert.IsTrue(hbeat.AddHealthProperty(testerKey, "test", true));
                hbeat.Initialize(configuration: config);

                MetricTelemetry payload = (MetricTelemetry)hbeat.GatherData();

                Assert.IsTrue(payload.Properties.Any(
                    a => a.Key.Equals(testerKey, StringComparison.OrdinalIgnoreCase)),
                    "Provided custom payload provider to heartbeat but never received any messages with its content");
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFields()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                List<string> disableHbProps = new List<string>();

                for (int i = 0; i < HeartbeatDefaultPayload.DefaultFields.Length; ++i)
                {
                    if (i % 2 == 0)
                    {
                        disableHbProps.Add(HeartbeatDefaultPayload.DefaultFields[i]);
                        hbeat.DisabledDefaultFields.Add(HeartbeatDefaultPayload.DefaultFields[i]);
                    }
                }

                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                var sentHeartBeat = (MetricTelemetry)hbeat.GatherData();

                Assert.IsNotNull(sentHeartBeat);

                foreach (var kvp in sentHeartBeat.Properties)
                {
                    Assert.IsFalse(disableHbProps.Contains(kvp.Key), 
                        string.Format(CultureInfo.CurrentCulture, "Dissallowed field '{0}' found in payload", kvp.Key));
                }
            }
        }

        [TestMethod]
        public void HeartbeatMetricIsZeroForNoFailureConditionPresent()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);
                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsFalse(msg.Sum > 0.0);
            }
        }

        [TestMethod]
        public void HeartbeatMetricIsNonZeroWhenFailureConditionPresent()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);
                string testerKey = "tester123";
                hbeat.AddHealthProperty(testerKey, "test", false);

                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsTrue(msg.Sum >= 1.0);
            }
        }

        [TestMethod]
        public void HeartbeatMetricCountAccountsForAllFailures()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsTrue(msg.Sum == 0.0);

                hbeat.AddHealthProperty("tester01", "test failure 1", false);
                hbeat.AddHealthProperty("tester02", "test failure 2", false);
                msg = (MetricTelemetry)hbeat.GatherData();

                Assert.IsTrue(msg.Sum == 2.0);
            }
        }

        [TestMethod]
        public void SentHeartbeatContainsExpectedDefaultFields()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsNotNull(msg);

                foreach (string field in HeartbeatDefaultPayload.DefaultFields)
                {
                    try
                    {
                        var fieldPayload = msg.Properties.Single(a => string.Compare(a.Key, field) == 0);
                        Assert.IsNotNull(fieldPayload);
                        if (field.Equals(HeartbeatDefaultPayload.UpdatedFieldsPropertyKey, StringComparison.OrdinalIgnoreCase))
                        {
                            Assert.IsTrue(string.IsNullOrEmpty(fieldPayload.Value)); // updated fields should be empty for sdk default fields
                        }
                        else
                        {
                            Assert.IsFalse(string.IsNullOrEmpty(fieldPayload.Value)); // updated fields should not be empty
                        }

                    }
                    catch (Exception)
                    {
                        Assert.Fail(string.Format(CultureInfo.CurrentCulture, "The default field '{0}' is not present exactly once in a sent message.", field));
                    }
                }
            }
        }

        [TestMethod]
        public void PayloadExtensionHandlesExtensionPayloadNameCollision()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsTrue(hbeat.AddHealthProperty("test01", "some test value", true));
                Assert.IsFalse(hbeat.AddHealthProperty("test01", "some other test value", true));
            }
        }

        [TestMethod]
        public void CannotSetPayloadExtensionWithoutAddingItFirst()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsFalse(hbeat.SetHealthProperty("test01", "some other test value", true));
                Assert.IsTrue(hbeat.AddHealthProperty("test01", "some test value", true));
                Assert.IsTrue(hbeat.SetHealthProperty("test01", "some other test value", true));
            }
        }

        [TestMethod]
        public void CannotSetValueOfDefaultPayloadProperties()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                foreach (string key in HeartbeatDefaultPayload.DefaultFields)
                {
                    Assert.IsFalse(hbeat.SetHealthProperty(key, "test", true));
                }
            }
        }

        [TestMethod]
        public void CannotAddPayloadItemNamedOfDefaultPayloadProperties()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                foreach (string key in HeartbeatDefaultPayload.DefaultFields)
                {
                    Assert.IsFalse(hbeat.AddHealthProperty(key, "test", true));
                }
            }
        }

        [TestMethod]
        public void EnsureAllTargetFrameworksRepresented()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                var msg = (MetricTelemetry)hbeat.GatherData();

                Assert.IsTrue(msg.Properties.ContainsKey("baseSdkTargetFramework"));
                Assert.IsFalse(msg.Properties["baseSdkTargetFramework"].Equals("undefined", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public void CanSetHealthHeartbeatPayloadValueWithoutHealthyFlag()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                string key = "setValueTest";

                Assert.IsTrue(hbeat.AddHealthProperty(key, "value01", true));
                Assert.IsTrue(hbeat.SetHealthProperty(key, "value02"));
                var msg = (MetricTelemetry)hbeat.GatherData();
                
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.Properties.ContainsKey(key));
                Assert.IsTrue(msg.Properties[key].Equals("value02", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void CanSetHealthHeartbeatPayloadHealthIndicatorWithoutSettingValue()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                var config = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
                hbeat.Initialize(configuration: config);

                string key = "healthSettingTest";

                Assert.IsTrue(hbeat.AddHealthProperty(key, "value01", true));
                Assert.IsTrue(hbeat.SetHealthProperty(key, null, false));
                var msg = (MetricTelemetry)hbeat.GatherData();
                
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.Properties.ContainsKey(key));
                Assert.IsTrue(msg.Properties[key].Equals("value01", StringComparison.Ordinal));
                Assert.IsTrue(msg.Sum == 1.0); // one false message in payload only
            }
        }
    }
}

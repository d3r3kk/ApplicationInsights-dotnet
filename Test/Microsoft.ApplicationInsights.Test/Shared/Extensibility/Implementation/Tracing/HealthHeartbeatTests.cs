﻿namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TaskEx = System.Threading.Tasks.Task;

    [TestClass]
    class HealthHeartbeatTests
    {
        [TestMethod]
        public void InitializeHealthHeartbeat()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatTwiceDoesntFail()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultInterval()
        {
            using (var hbeat = new HealthHeartbeatProvider(1200))
            {
                hbeat.Initialize(configuration: null);
                Assert.AreEqual(1200,hbeat.HeartbeatIntervalMs);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultFieldsToEnable()
        {
            string specificFieldsToEnable = "osType,name";

            using (var hbeat = new HealthHeartbeatProvider(specificFieldsToEnable))
            {
                hbeat.Initialize(configuration: null);
                Assert.AreEqual(0, String.CompareOrdinal(hbeat.EnabledPayloadFields, specificFieldsToEnable));
            }
        }

        public class TestHeartbeatPayload : IHealthHeartbeatPayloadExtension
        {
            public Stack<KeyValuePair<string, object>> customProperties = new Stack<KeyValuePair<string, object>>();
            public int currentUnhealthyCount = 0;

            public TestHeartbeatPayload()
            {
            }

            public IEnumerable<KeyValuePair<string, object>> GetPayloadProperties()
            {
                return this.customProperties.ToArray();
            }

            public int CurrentUnhealthyCount => this.GetUnhealthyCountAndReset();

            public string Name => "TestHeartbeatPayload";

            private int GetUnhealthyCountAndReset()
            {
                int unhealthyCountThisTime = this.currentUnhealthyCount;
                this.currentUnhealthyCount = 0;
                return unhealthyCountThisTime;
            }
        }

        [TestMethod]
        public void CanExtendHeartbeatPayload()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                TestHeartbeatPayload payloadProperties = new TestHeartbeatPayload();
                hbeat.RegisterHeartbeatPayload(payloadProperties);
                
            }
        }

        [TestMethod]
        [Ignore]
        public void CanSetDelayBetweenHeartbeats()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void CanSetDelayBetweenHeartbeatsViaConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void DiagnosticsTelemetryModuleCreatesHeartbeatModule()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsDataByDefault()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFields()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFieldsSpecifiedInConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatMetricIsZeroForNoFailureConditionPresent()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatMetricIsNonZeroWhenFailureConditionPresent()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatSentAtProperIntervals()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatSentToMultipleConfiguredComponents()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HealthHeartbeatDisabledInConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatMetricCountAccountsForAllFailures()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void SentHeartbeatContainsExpectedDefaultFields()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void PayloadExtensionHandlesSingleFieldNameCollision()
        {
            string fieldName1 = "osType";

            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void PayloadExtensionHandlesMultipleFieldNameCollision()
        {
            string fieldName1 = "payloadEx";
            string fieldName2 = "payloadEx";

            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DefaultPayloadIncludesAppInsightsSdkVersion()
        {
            var defaultPayload = new HealthHeartbeatDefaultPayload("*");
            var defaultProps = defaultPayload.GetPayloadProperties();
            Assert.IsTrue(
                defaultProps.Any(a =>
                { return a.Key.Equals(HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer, StringComparison.Ordinal); }));
        }

        [TestMethod]
        public void DefaultPayloadIncludesOnlySpecifiedProperties()
        {
            string allowedProps = string.Concat(HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer, ",", HealthHeartbeatDefaultPayload.FieldTargetFramework);
            var defaultPayload = new HealthHeartbeatDefaultPayload(allowedProps);
            var defaultProps = defaultPayload.GetPayloadProperties();
            Assert.IsTrue(
                defaultProps.All(a =>
                {
                    return a.Key.Equals(HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer, StringComparison.Ordinal)
                      ||
                      a.Key.Equals(HealthHeartbeatDefaultPayload.FieldTargetFramework, StringComparison.Ordinal);
                }));
        }

    }
}

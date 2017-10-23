﻿namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Use diagnostics telemetry module to report SDK internal problems to the portal and VS debug output window.
    /// </summary>
    public sealed class DiagnosticsTelemetryModule : ITelemetryModule, IDisposable
    {
        internal readonly IList<IDiagnosticsSender> Senders = new List<IDiagnosticsSender>();

        internal readonly DiagnosticsListener EventListener;

        internal IHeartbeatProvider HeartbeatProvider = null;
        private readonly object lockObject = new object();
        private readonly IDiagnoisticsEventThrottlingScheduler throttlingScheduler = new DiagnoisticsEventThrottlingScheduler();
        private volatile bool disposed = false;
        private string instrumentationKey;
        private bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsTelemetryModule"/> class. 
        /// </summary>
        public DiagnosticsTelemetryModule()
        {
            // Adding a dummy queue sender to keep the data to be sent to the portal before the initialize method is called
            this.Senders.Add(new PortalDiagnosticsQueueSender());

            this.EventListener = new DiagnosticsListener(this.Senders);

            this.MillisecondsBetweenHeartbeats = HealthHeartbeatProvider.DefaultHeartbeatIntervalMs;
            this.AllowedHeartbeatProperties = HealthHeartbeatProvider.DefaultAllowedFieldsInHeartbeatPayload;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DiagnosticsTelemetryModule" /> class.
        /// </summary>
        ~DiagnosticsTelemetryModule()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the delay between heartbeats in milliseconds.
        /// </summary>
        public int MillisecondsBetweenHeartbeats { get; set; }

        /// <summary>
        /// Gets or sets property names that are allowed to be sent along with the health heartbeats. * means allow all default properties through.
        /// </summary>
        public string AllowedHeartbeatProperties { get; set; }

        /// <summary>
        /// Gets or sets diagnostics Telemetry Module LogLevel configuration setting. 
        /// Possible values LogAlways, Critical, Error, Warning, Informational and Verbose.
        /// </summary>
        public string Severity
        {
            get
            {
                return this.EventListener.LogLevel.ToString();
            }

            set
            {
                // Once logLevel is set from configuration, restart listener with new value
                if (!string.IsNullOrEmpty(value))
                {
                    EventLevel parsedValue;
                    if (Enum.IsDefined(typeof(EventLevel), value) == true)
                    {
                        parsedValue = (EventLevel)Enum.Parse(typeof(EventLevel), value, true);
                        this.EventListener.LogLevel = parsedValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets instrumentation key for diagnostics. Use to redirect SDK 
        /// internal problems reporting to the separate instrumentation key.
        /// </summary>
        public string DiagnosticsInstrumentationKey
        {
            get
            {
                return this.instrumentationKey;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.instrumentationKey = value;

                    // Set instrumentation key in Portal sender
                    foreach (var portalSender in this.Senders.OfType<PortalDiagnosticsSender>())
                    {
                        portalSender.DiagnosticsInstrumentationKey = this.instrumentationKey;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes this telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry configuration to use for this telemetry module.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Temporary fix to make sure that we initialize module once.
            // It should be removed when configuration reading logic is moved to Web SDK.
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        var queueSender = this.Senders.OfType<PortalDiagnosticsQueueSender>().First();
                        queueSender.IsDisabled = true;
                        this.Senders.Remove(queueSender);
                        
                        PortalDiagnosticsSender portalSender = new PortalDiagnosticsSender(
                            configuration,
                            new DiagnoisticsEventThrottlingManager<DiagnoisticsEventThrottling>(
                                new DiagnoisticsEventThrottling(DiagnoisticsEventThrottlingDefaults.DefaultThrottleAfterCount),
                                this.throttlingScheduler,
                                DiagnoisticsEventThrottlingDefaults.DefaultThrottlingRecycleIntervalInMinutes));
                        portalSender.DiagnosticsInstrumentationKey = this.DiagnosticsInstrumentationKey;

                        this.Senders.Add(portalSender);

                        foreach (TraceEvent traceEvent in queueSender.EventData)
                        {
                            portalSender.Send(traceEvent);
                        }

                        // set up heartbeat
                        if (this.HeartbeatProvider == null)
                        {
                            this.HeartbeatProvider = new HealthHeartbeatProvider();
                        }

                        this.HeartbeatProvider.Initialize(configuration, this.MillisecondsBetweenHeartbeats, this.AllowedHeartbeatProperties);

                        this.isInitialized = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// Allows consumers of the DiagosticsTelemetryModule register an extension for the health heartbeat payload.
        /// 
        /// This can also be done by adding the IHealthHeartbeatProperty implementation within the ApplicationInsights.config
        /// file.
        /// </summary>
        /// <param name="payloadProvider">Extension payload to include in Health Heartbeat payloads</param>
        public void RegisterHeartbeatPayload(IHealthHeartbeatPayloadExtension payloadProvider)
        {
            if (this.HeartbeatProvider != null)
            {
                this.HeartbeatProvider.RegisterHeartbeatPayload(payloadProvider);
            }
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="managed">Indicates if managed code is being disposed.</param>
        private void Dispose(bool managed)
        {
            if (managed && !this.disposed)
            {
                this.EventListener.Dispose();
                (this.throttlingScheduler as IDisposable).Dispose();
                foreach (var disposableSender in this.Senders.OfType<IDisposable>())
                {
                    disposableSender.Dispose();
                }

                this.HeartbeatProvider = null;

                GC.SuppressFinalize(this);
            }

            this.disposed = true;
        }
    }
}

// -----------------------------------------------------------------------
// Copyright 2013 Joe Fitzgerald
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loggly;
using Loggly.Config;
using Loggly.Transports.Syslog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;

namespace NLog.Targets
{
    /// <summary>
    /// A Loggly target for NLog
    /// </summary>
    [Target("Loggly")]
    public class LogglyTarget : TargetWithContext
    {
        private ILogglyClient _client;
        internal Func<ILogglyClient> ClientFactory { get; set; }
        private int _pendingTaskCount;
        private readonly Action<Task<LogResponse>> _receivedLogResponse;

        /// <summary>
        /// Number of logevents to include in a single batch-task
        /// </summary>
        public int BatchPostingLimit { get; set; }

        /// <summary>
        /// Number of pending batch-tasks before blocking
        /// </summary>
        public int TaskPendingLimit { get; set; }

        /// <summary>
        /// Gets the array of custom tags to be passed with the logevent
        /// </summary>
        [ArrayParameter(typeof(LogglyTagProperty), "tag")]
        public IList<LogglyTagProperty> Tags { get; } = new List<LogglyTagProperty>();

        /// <summary>
        /// Gets the array of custom attributes to be passed into the logevent context
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [ArrayParameter(typeof(TargetPropertyWithContext), "contextproperty")]
        public override IList<TargetPropertyWithContext> ContextProperties { get; } = new List<TargetPropertyWithContext>();

        /// <summary>
        /// 
        /// </summary>
        public Layout ApplicationName { get; set; } = "${appdomain:cached=true:format=\\{1\\}}";

        /// <summary>
        /// Loggly Customer Token
        /// </summary>
        public Layout CustomerToken { get; set; }

        /// <summary>
        /// Loggly EndPoint HostName
        /// </summary>
        public Layout EndpointHostname { get; set; }

        /// <summary>
        /// Loggly EndPoint Host PortNumber
        /// </summary>
        public Layout EndpointPort { get; set; }

        /// <summary>
        /// Loggly EndPoint Protocol (Https, SyslogSecure, SyslogUdp, SyslogTcp)
        /// </summary>
        public LogTransport LogTransport { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="LogglyTarget"/>.
        /// </summary>
        public LogglyTarget()
        {
            ClientFactory = () => new LogglyClient();
            OptimizeBufferReuse = true;
            IncludeEventProperties = true;
            BatchPostingLimit = 10;
            TaskPendingLimit = 5;
            _receivedLogResponse = ReceivedLogResponse;
        }

        /// <inheritdoc />
        protected override void InitializeTarget()
        {
            var customerToken = CustomerToken?.Render(LogEventInfo.CreateNullEvent());
            if (!string.IsNullOrWhiteSpace(customerToken))
            {
                LogglyConfig.Instance.CustomerToken = customerToken;

                var applicationName = ApplicationName?.Render(LogEventInfo.CreateNullEvent());
                if (!string.IsNullOrWhiteSpace(applicationName))
                {
                    LogglyConfig.Instance.ApplicationName = applicationName;
                }
            }

            var endPointHostName = EndpointHostname?.Render(LogEventInfo.CreateNullEvent());
            if (!string.IsNullOrWhiteSpace(endPointHostName))
            {
                var endpointPort = EndpointPort?.Render(LogEventInfo.CreateNullEvent());
                if (!int.TryParse(endpointPort ?? string.Empty, out var endpointPortNumber))
                {
                    endpointPortNumber = 0; // Let Loggly guess from LogTransport-enum
                }

                LogglyConfig.Instance.Transport = new TransportConfiguration()
                {
                    EndpointHostname = endPointHostName,
                    EndpointPort = endpointPortNumber,
                    LogTransport = LogTransport,
                }.GetCoercedToValidConfig();
            }

            base.InitializeTarget();
            _pendingTaskCount = 0;
            _client = ClientFactory.Invoke();
        }

        /// <inheritdoc />
        protected override void CloseTarget()
        {
            _client = null;
            base.CloseTarget();
        }

        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent)
        {
            var logglyEvent = ConvertToLogglyEvent(logEvent);
            if (logglyEvent != null)
            {
                var task = _client.Log(logglyEvent).ContinueWith(_receivedLogResponse);
                if (Interlocked.Increment(ref _pendingTaskCount) >= TaskPendingLimit)
                {
                    task.Wait();
                    _pendingTaskCount = 0;
                }
            }
        }

        /// <inheritdoc />
        protected override void Write(IList<AsyncLogEventInfo> logEvents)
        {
            if (BatchPostingLimit <= 1 || logEvents.Count <= 1)
            {
                base.Write(logEvents);
            }
            else
            {
                int index = 0;
                try
                {
                    for (index = 0; index < logEvents.Count; index += BatchPostingLimit)
                    {
                        int logCount = Math.Min(logEvents.Count - index, BatchPostingLimit);
                        List<LogglyEvent> logglyEvents = new List<LogglyEvent>(logCount);
                        for (int i = 0; i < logCount; ++i)
                        {
                            var logglyEvent = ConvertToLogglyEvent(logEvents[index + i].LogEvent);
                            if (logglyEvent != null)
                                logglyEvents.Add(logglyEvent);
                        }
                        if (logglyEvents.Count > 0)
                        {
                            var task = _client.Log(logglyEvents).ContinueWith(_receivedLogResponse);
                            if (Interlocked.Increment(ref _pendingTaskCount) >= TaskPendingLimit)
                            {
                                task.Wait();
                                _pendingTaskCount = 0;
                            }
                        }
                        for (int i = 0; i < logCount; ++i)
                        {
                            logEvents[index + i].Continuation(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    InternalLogger.Error(ex, "Loggly - {0}", ex.Message);
                    for (; index < logEvents.Count; ++index)
                    {
                        logEvents[index].Continuation(ex);
                    }
                }
            }
        }

        private void ReceivedLogResponse(Task<LogResponse> logResponse)
        {
            Interlocked.Decrement(ref _pendingTaskCount);
            if (logResponse.IsFaulted && logResponse.Exception != null)
            {
                InternalLogger.Error("Loggly - {0}", logResponse.Exception.ToString());
            }
            else if (logResponse.Status == TaskStatus.RanToCompletion)
            {
                if (logResponse.Result.Code == ResponseCode.Error)
                {
                    InternalLogger.Error("Loggly - {0}", logResponse.Result.Message);
                }
            }
        }

        /// <inheritdoc />
        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            for (int i = 0; i < 3000; ++i)
            {
                if (_pendingTaskCount == 0)
                {
                    asyncContinuation(null);
                    return;
                }

                Thread.Sleep(10);
            }

            asyncContinuation(new TimeoutException($"LogglyClient with {_pendingTaskCount} pending tasks"));
        }

        private LogglyEvent ConvertToLogglyEvent(LogEventInfo logEvent)
        {
            if (logEvent.HasProperties && logEvent.Properties.ContainsKey("syslog-suppress"))
            {
                /*
                 * logging delimiting messages like "--------------" makes sense for pretty printing to file log targets
                 * but not so much for loggly. Support suppression.
                 */
                return null;
            }

            var logMessage = RenderLogEvent(Layout, logEvent) ?? string.Empty;

            var logglyEvent = new LogglyEvent();
            var isHttpTransport = LogglyConfig.Instance.Transport.LogTransport == LogTransport.Https;

            logglyEvent.Timestamp = logEvent.TimeStamp;
            logglyEvent.Syslog.MessageId = logEvent.SequenceID;
            logglyEvent.Syslog.Level = ToSyslogLevel(logEvent.Level);

            if (logEvent.Exception != null)
            {
                logglyEvent.Data.Add("exception", (object)logEvent.Exception);
            }

            if (isHttpTransport)
            {
                // syslog will capture these via the header
                logglyEvent.Data.Add("sequenceId", (object)logEvent.SequenceID);
                logglyEvent.Data.Add("level", (object)logEvent.Level.Name);
            }

            for (int i = 0; i < Tags.Count; ++i)
            {
                string tagName = RenderLogEvent(Tags[i].Name, logEvent);
                if (!string.IsNullOrEmpty(tagName))
                {
                    logglyEvent.Options.Tags.Add(tagName);
                }
            }

            logglyEvent.Data.Add("message", (object)logMessage);

            if (ShouldIncludeProperties(logEvent))
            {
                var properties = GetAllProperties(logEvent);
                foreach (var prop in properties)
                {
                    if (string.IsNullOrEmpty(prop.Key))
                        continue;

                    logglyEvent.Data.AddIfAbsent(prop.Key, prop.Value);
                }
            }
            else
            {
                for (int i = 0; i < ContextProperties.Count; ++i)
                {
                    var contextKey = ContextProperties[i].Name;
                    if (string.IsNullOrEmpty(contextKey))
                        continue;

                    var contextValue = RenderLogEvent(ContextProperties[i].Layout, logEvent);
                    if (string.IsNullOrEmpty(contextValue) && !ContextProperties[i].IncludeEmptyValue)
                        continue;

                    logglyEvent.Data.AddIfAbsent(contextKey, contextValue);
                }
            }

            return logglyEvent;
        }


        private Level ToSyslogLevel(LogLevel nLogLevel)
        {
            switch (nLogLevel.Name)
            {
                case "Debug": return Level.Debug;
                case "Error": return Level.Error;
                case "Fatal": return Level.Critical;
                case "Info": return Level.Information;
                case "Trace": return Level.Debug; // syslog doesn't have anything below debug. Mashed debug and trace together
                case "Warn": return Level.Warning;
            }

            InternalLogger.Error("Loggly - Unknown LogLevel {0}", nLogLevel.Name);
            return Level.Alert;
        }
    }
}
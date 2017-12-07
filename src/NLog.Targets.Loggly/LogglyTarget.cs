// -----------------------------------------------------------------------
// <copyright file="Loggly.cs">
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

namespace NLog.Targets
{
    /// <summary>
    /// A Loggly target for NLog
    /// </summary>
    [Target("Loggly")]
    public class LogglyTarget : TargetWithLayout
    {
        internal ILogglyClient _client;
        private int _pendingTaskCount;
        private readonly Action<Task<LogResponse>> _receivedLogResponse;

        public int BatchPostingLimit { get; set; }
        public int TaskPendingLimit { get; set; }

        public LogglyTarget()
        {
            BatchPostingLimit = 10;
            TaskPendingLimit = 5;
            _receivedLogResponse = ReceivedLogResponse;
        }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _pendingTaskCount = 0;
            _client = new LogglyClient();
        }

        protected override void CloseTarget()
        {
            _client = null;
            base.CloseTarget();
        }

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

        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            if (logEvents.Length == 1)
            {
                Write(logEvents[0].LogEvent);
                logEvents[0].Continuation(null);
            }
            else if (logEvents.Length > 0)
            {
                int index = 0;
                try
                {
                    for (index = 0; index < logEvents.Length; index += BatchPostingLimit)
                    {
                        int logCount = Math.Min(logEvents.Length - index, BatchPostingLimit);
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
                    InternalLogger.Error("Loggly - {0}", ex.ToString());
                    for (index = 0; index < logEvents.Length; ++index)
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

        public LogglyEvent ConvertToLogglyEvent(LogEventInfo logEvent)
        {
            // The unwrapped event has a zero sequenceId, grab it before unwrapping;
            var sequenceId = logEvent.SequenceID;

            logEvent = GetCorrectEvent(logEvent);

            if (logEvent.Properties.ContainsKey("syslog-suppress"))
            {
                /*
                 * logging delimiting messages like "--------------" makes sense for pretty printing to file log targets
                 * but not so much for loggly. Support suppression.
                 */
                return null;
            }

            var logMessage = Layout.Render(logEvent);

            var logglyEvent = new LogglyEvent();
            var isHttpTransport = LogglyConfig.Instance.Transport.LogTransport == LogTransport.Https;

            logglyEvent.Timestamp = logEvent.TimeStamp;
            logglyEvent.Syslog.MessageId = sequenceId;
            logglyEvent.Syslog.Level = ToSyslogLevel(logEvent.Level);

            if (logEvent.Exception != null)
            {
                logglyEvent.Data.Add("exception", (object)logEvent.Exception);
            }

            if (isHttpTransport)
            {
                // syslog will capture these via the header
                logglyEvent.Data.Add("sequenceId", (object)sequenceId);
                logglyEvent.Data.Add("level", (object)logEvent.Level.Name);
            }

            logglyEvent.Data.Add("message", (object)logMessage);
            if (logEvent.Properties.Count > 0)
            {
                foreach (var prop in logEvent.Properties)
                {
                    logglyEvent.Data.AddIfAbsent(prop.Key.ToString(), prop.Value);
                }
            }

            return logglyEvent;
        }

        /// <summary>
        /// Async nLog wraps the original event in a new one (not sure why?)
        /// Unwrap the original and use that so we can get all our parameters
        /// http://stackoverflow.com/questions/23272439/nlog-event-contextitem-xxxx-not-writing-in-logging-database
        /// </summary>
        public static LogEventInfo GetCorrectEvent(LogEventInfo inboundEventInfo)
        {
            LogEventInfo outputEventInfo = inboundEventInfo;
            if (inboundEventInfo.Parameters != null && inboundEventInfo.Parameters.Length == 1)
            {
                var nestedEvent = inboundEventInfo.Parameters[0] as LogEventInfo;
                if (nestedEvent != null)
                {
                    outputEventInfo = nestedEvent;
                    outputEventInfo.Level = inboundEventInfo.Level;
                }
            }
            return outputEventInfo;
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
                default: LogglyException.Throw("Failed to map level"); return Level.Alert;
            }
        }
    }
}
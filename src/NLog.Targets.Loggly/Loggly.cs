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
using System.Linq;
using Loggly;
using Loggly.Config;
using Loggly.Transports.Syslog;
using NLog.Config;

namespace NLog.Targets
{
    /// <summary>
    /// A Loggly target for NLog
    /// </summary>
    [Target("Loggly")]
    public class Loggly : TargetWithLayout
    {

        protected override void Write(LogEventInfo logEvent)
        {
            var loggly = new LogglyClient();
            
            // The unwrapped event has a zero sequenceId, grab it before unwrapping;
            var sequenceId = logEvent.SequenceID;

            logEvent = GetCorrectEvent(logEvent);

            if (logEvent.Properties.ContainsKey("syslog-suppress"))
            {
                /*
                 * logging delimiting messages like "--------------" makes sense for pretty printing to file log targets
                 * but not so much for loggly. Support suppression.
                 */
                return;
            }

            var logMessage = Layout.Render(logEvent);

            var logglyEvent = new LogglyEvent();
            var isHttpTransport = LogglyConfig.Instance.Transport.LogTransport == LogTransport.Https;

            logglyEvent.Timestamp = logEvent.TimeStamp;
            logglyEvent.Syslog.MessageId = sequenceId;
            logglyEvent.Syslog.Level = ToSyslogLevel(logEvent.Level); 
            
            if (logEvent.Exception != null)
            {
                logglyEvent.Data.Add("exception", logEvent.Exception);
            }

            if (isHttpTransport)
            {
                // syslog will capture these via the header
                logglyEvent.Data.Add("sequenceId", sequenceId);
                logglyEvent.Data.Add("level", logEvent.Level.Name);
            }

            logglyEvent.Data.Add("message", logMessage);
            foreach (var key in logEvent.Properties.Keys)
            {
                logglyEvent.Data.AddSafe(key.ToString(), logEvent.Properties[key]);
            }
            
            loggly.Log(logglyEvent);
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
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
            var logMessage = Layout.Render(logEvent);

            var options = new MessageOptions();
            var isHttpTransport = LogglyConfig.Instance.Transport.LogTransport == LogTransport.Https;

            // for syslog
            options.MessageId = sequenceId;
            options.Level = ToSyslogLevel(logEvent.Level);
            
            if (logEvent.Properties != null)
            {
                if (logEvent.Exception != null)
                {
                    AddPropertyIfNotExists(logEvent, "exception", logEvent.Exception);
                }
                if (isHttpTransport)
                {
                    // syslog will capture these via options
                    AddPropertyIfNotExists(logEvent, "sequenceId", sequenceId);
                    AddPropertyIfNotExists(logEvent, "level", logEvent.Level.Name);
                    AddPropertyIfNotExists(logEvent, "timestamp", DateTime.Now.ToSyslog()); //https://www.loggly.com/docs/automated-parsing/#json
                }
                AddProperty(logEvent, "message", logMessage);

                var propertyDictionary = logEvent.Properties.ToDictionary(k => k.Key != null ? k.Key.ToString() : string.Empty, v => v.Value);
                loggly.Log(options, propertyDictionary);
            }
            else
            {
                loggly.Log(logMessage); // should see a properties object but just in case
            }
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

        private void AddPropertyIfNotExists(LogEventInfo eventInfo, string name, object value)
        {
            if (!eventInfo.Properties.ContainsKey(name))
            {
                AddProperty(eventInfo, name, value);
            }
        }

        private void AddProperty(LogEventInfo eventInfo, string name, object value)
        {
            eventInfo.Properties.Add(new KeyValuePair<object, object>(name, value));
        }
    }
}
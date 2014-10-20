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
            var logMessage = Layout.Render(logEvent);

            if (logEvent.Properties != null)
            {
                AddPropertyIfNotExists(logEvent, "host", Environment.MachineName);
                AddPropertyIfNotExists(logEvent, "sequenceID", logEvent.SequenceID);

                if (logEvent.Exception != null)
                {
                    AddPropertyIfNotExists(logEvent, "exception", logEvent.Exception);
                }

                var propertyDictionary = logEvent.Properties.ToDictionary(k => k.Key != null ? k.Key.ToString() : string.Empty, v => v.Value);
                loggly.Log(logMessage, logEvent.Level.Name, propertyDictionary);
            }
            else
            {
                loggly.Log(logMessage, logEvent.Level.Name);
            }
        }

        private void AddPropertyIfNotExists(LogEventInfo eventInfo, string name, object value)
        {
            if (!eventInfo.Properties.ContainsKey(name))
            {
                eventInfo.Properties.Add(new KeyValuePair<object, object>(name, value));
            }
        }
    }
}
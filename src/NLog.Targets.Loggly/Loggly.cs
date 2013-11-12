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

using System.Linq;
using NLog.Config;
using LogglyLogger = Loggly.Logger;

namespace NLog.Targets
{
    /// <summary>
    /// A Loggly NLog Target
    /// </summary>
    [Target("Loggly")]
    public class Loggly : TargetWithLayout
    {
        [RequiredParameter]
        public string InputKey { get; set; }

        public string AlternativeUrl { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            var logger = new LogglyLogger(InputKey, string.IsNullOrEmpty(AlternativeUrl) ? null : string.Format("{0}/", AlternativeUrl.TrimEnd('/')));
            var logMessage = Layout.Render(logEvent);
            if (logEvent.Properties != null && logEvent.Properties.Count > 0)
            {
                logger.Log(logMessage, logEvent.Level.Name, logEvent.Properties.ToDictionary(k => k.Key != null ? k.Key.ToString() : string.Empty, v => v.Value));
            }
            else
            {
                logger.Log(logMessage, logEvent.Level.Name);
            }
        }
    }
}
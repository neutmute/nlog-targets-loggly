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

using System.Collections.Generic;
using System.Threading.Tasks;
using Loggly;

namespace NLog.Targets.Loggly.Tests
{
    class LogglyClientMock : ILogglyClient
    {
        public List<LogglyEvent> LogglyEvents { get; set; } = new List<LogglyEvent>(1000);

        public Task<LogResponse> Log(LogglyEvent logglyEvent)
        {
            LogglyEvents.Add(logglyEvent);
            return Task.FromResult(new LogResponse() { Code = ResponseCode.Success });
        }

        public Task<LogResponse> Log(IEnumerable<LogglyEvent> logglyEvents)
        {
            LogglyEvents.AddRange(logglyEvents);
            return Task.FromResult(new LogResponse() { Code = ResponseCode.Success });
        }
    }
}

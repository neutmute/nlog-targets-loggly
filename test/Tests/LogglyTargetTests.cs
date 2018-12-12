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

using System.IO;
using System.Reflection;
using System.Xml;
using NLog.Config;
using NUnit.Framework;

namespace NLog.Targets.Loggly.Tests
{
    [TestFixture]
    public class LogglyTargetTests
    {
        [Test]
        public void LogglyTargetSimpleTest()
        {
            NLog.LogFactory logFactory = new NLog.LogFactory();
            NLog.Config.LoggingConfiguration logConfig = CreateConfigurationFromString(
                @"<nlog throwExceptions='true'>
                    <extensions>
	                    <add assembly='NLog.Targets.Loggly' />
                    </extensions>
                    <targets>
                        <target name='Loggly' type='Loggly' layout='${message}' />
                    </targets>
	                <rules>
	                    <logger name='*' minlevel='Info' writeTo='Loggly' />
                    </rules>
                  </nlog>", logFactory);
            var logglyTarget = logConfig.FindTargetByName("Loggly") as NLog.Targets.LogglyTarget;
            var logglyClientMock = new LogglyClientMock();
            logglyTarget.ClientFactory = () => logglyClientMock;
            logFactory.Configuration = logConfig;
            NLog.Logger logger = logFactory.GetLogger(MethodInfo.GetCurrentMethod().Name);
            logger.Info("{{\"aaa}}");
            Assert.AreEqual(1, logglyClientMock.LogglyEvents.Count);
        }

        [Test]
        public void LogglyTargetTagsTest()
        {
            NLog.LogFactory logFactory = new NLog.LogFactory();
            NLog.Config.LoggingConfiguration logConfig = CreateConfigurationFromString(
                @"<nlog throwExceptions='true'>
                    <extensions>
	                    <add assembly='NLog.Targets.Loggly' />
                    </extensions>
                    <targets>
                        <target name='Loggly' type='Loggly' layout='${message}'>
                            <tag name='hello' />
                            <tag name='${logger}' />
                        </target>
                    </targets>
	                <rules>
	                    <logger name='*' minlevel='Info' writeTo='Loggly' />
                    </rules>
                  </nlog>", logFactory);
            var logglyTarget = logConfig.FindTargetByName("Loggly") as NLog.Targets.LogglyTarget;
            var logglyClientMock = new LogglyClientMock();
            logglyTarget.ClientFactory = () => logglyClientMock;
            logFactory.Configuration = logConfig;
            NLog.Logger logger = logFactory.GetLogger(MethodInfo.GetCurrentMethod().Name);
            logger.Info("Hello World");
            Assert.AreEqual(1, logglyClientMock.LogglyEvents.Count);
            Assert.AreEqual(2, logglyClientMock.LogglyEvents[0].Options.Tags.Count);
            Assert.AreEqual("hello", logglyClientMock.LogglyEvents[0].Options.Tags[0].Value);
            Assert.AreEqual(MethodInfo.GetCurrentMethod().Name, logglyClientMock.LogglyEvents[0].Options.Tags[1].Value);
        }

        [Test]
        public void LogglyTargetContextPropertyTest()
        {
            NLog.LogFactory logFactory = new NLog.LogFactory();
            NLog.Config.LoggingConfiguration logConfig = CreateConfigurationFromString(
                @"<nlog throwExceptions='true'>
                    <extensions>
	                    <add assembly='NLog.Targets.Loggly' />
                    </extensions>
                    <targets>
                        <target name='Loggly' type='Loggly' layout='${message}'>
                            <contextproperty name='hello' layout='${logger}' />
                        </target>
                    </targets>
	                <rules>
	                    <logger name='*' minlevel='Info' writeTo='Loggly' />
                    </rules>
                  </nlog>", logFactory);
            var logglyTarget = logConfig.FindTargetByName("Loggly") as NLog.Targets.LogglyTarget;
            var logglyClientMock = new LogglyClientMock();
            logglyTarget.ClientFactory = () => logglyClientMock;
            logFactory.Configuration = logConfig;
            NLog.Logger logger = logFactory.GetLogger(MethodInfo.GetCurrentMethod().Name);
            logger.Info("Hello World");
            Assert.AreEqual(1, logglyClientMock.LogglyEvents.Count);
            Assert.Contains("hello", logglyClientMock.LogglyEvents[0].Data.KeyList);
            Assert.AreEqual(MethodInfo.GetCurrentMethod().Name, logglyClientMock.LogglyEvents[0].Data["hello"]);
        }

        public static XmlLoggingConfiguration CreateConfigurationFromString(string configXml, NLog.LogFactory logFactory)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(configXml);

            using (var stringReader = new StringReader(doc.DocumentElement.OuterXml))
            {
                XmlReader reader = XmlReader.Create(stringReader);

                return new XmlLoggingConfiguration(reader, null, false, logFactory);
            }
        }
    }
}

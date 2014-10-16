using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Demo
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            // Normal logging
            Log.Info("Demo starting up");

            // Adding custom parameter
            var eventInfo = new LogEventInfo(LogLevel.Info, "MyLogName", "event info test message");
            eventInfo.Properties.Add("CustomParameter", 123456);
            Log.Log(eventInfo);

            // Logging an exception
            var eventInfo2 = new LogEventInfo(LogLevel.Error, "MyLogName", "something broke");
            eventInfo2.Exception = new Exception("This is a really bad exception");
            eventInfo2.Properties.Add("CustomParameter", 987654);
            Log.Log(eventInfo2);

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}

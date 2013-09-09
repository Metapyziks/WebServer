using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public class ScheduledJob
    {
        private Action job;

        public String Identifier { get; private set; }

        public DateTime NextTime { get; private set; }
        public TimeSpan Interval { get; private set; }

        public bool ShouldPerform
        {
            get { return DateTime.Now >= NextTime; }
        }

        public ScheduledJob(String ident, DateTime nextTime, TimeSpan interval, Action job)
        {
            Identifier = ident;

            NextTime = nextTime;
            Interval = interval;

            this.job = job;
        }

        internal void Perform()
        {
            NextTime = DateTime.Now + Interval;

            try {
                job();
            } catch (Exception e) {
                Console.WriteLine("[{0}] Exception thrown while performing scheduled job {1}:",
                    DateTime.Now, Identifier);
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}

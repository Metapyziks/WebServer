using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    internal class ScheduledJob
    {
        private Action job;

        internal String Identifier { get; private set; }

        internal DateTime NextTime { get; private set; }
        internal TimeSpan Interval { get; private set; }

        internal bool ShouldPerform
        {
            get { return DateTime.Now >= NextTime; }
        }

        internal ScheduledJob(String ident, DateTime nextTime, TimeSpan interval, Action job)
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
                Console.WriteLine("[{0}] Performing scheduled job {1}",
                    DateTime.Now, Identifier);
                job();
                Console.WriteLine("[{0}] Completed scheduled job {1}",
                    DateTime.Now, Identifier);
            } catch (Exception e) {
                Console.WriteLine("[{0}] {1}", DateTime.Now, e);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}

using System;

namespace WebServer
{
    internal class ScheduledJob
    {
        private Action<Server> job;

        internal String Identifier { get; private set; }

        internal DateTime NextTime { get; private set; }
        internal TimeSpan Interval { get; private set; }

        internal bool ShouldPerform
        {
            get { return DateTime.Now >= NextTime; }
        }

        internal bool OnceOnly
        {
            get { return Interval == TimeSpan.Zero; }
        }

        internal ScheduledJob(String ident, DateTime nextTime,
            TimeSpan interval, Action<Server> job)
        {
            Identifier = ident;

            NextTime = nextTime;
            Interval = interval;

            this.job = job;
        }

        internal void Perform(Server server)
        {
            if (!OnceOnly) {
                NextTime = DateTime.Now + Interval;
            } else {
                NextTime = DateTime.MaxValue;
            }

            try {
                server.Log("Performing {0}", Identifier);
                job(server);
                server.Log("Completed {0}", Identifier);
            } catch (Exception e) {
                server.Log(e);
            }
        }
    }
}

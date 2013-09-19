using System;
using System.Threading;

namespace WebServer
{
    internal class ScheduledJob
    {
        private Action<Server> job;

        internal String Identifier { get; private set; }

        internal DateTime NextTime { get; private set; }
        internal TimeSpan Interval { get; private set; }

        internal Timer Timer { get; private set; }

        internal bool ShouldPerform
        {
            get { return DateTime.Now >= NextTime; }
        }

        internal bool OnceOnly
        {
            get { return Interval == TimeSpan.Zero; }
        }

        internal ScheduledJob(Server server, String ident, DateTime nextTime,
            TimeSpan interval, Action<Server> job)
        {
            if (interval > TimeSpan.Zero) {
                while (nextTime < DateTime.Now) nextTime += interval;
            }

            Identifier = ident;

            NextTime = nextTime;
            Interval = interval;

            this.job = job;

            Timer = new Timer(Perform, server, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            UpdateTimer();
        }

        private void UpdateTimer()
        {
            var delay = NextTime - DateTime.Now;
            if (delay.TotalMinutes > 1.0) {
                delay = TimeSpan.FromMinutes(1.0);
            }

            Timer.Change(delay, Timeout.InfiniteTimeSpan);
        }

        private void Perform(Object state)
        {
            if (!ShouldPerform) {
                UpdateTimer();
                return;
            }

            var server = (Server) state;
            try {
                server.Log("Performing {0}", Identifier);
                job(server);
                server.Log("Completed {0}", Identifier);
            } catch (Exception e) {
                server.Log(e);
            }

            if (OnceOnly) {
                Cancel();
            } else {
                NextTime = DateTime.Now + Interval;
                UpdateTimer();
            }
        }

        internal void Cancel()
        {
            NextTime = DateTime.MaxValue;
            Timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            Timer.Dispose();
        }
    }
}

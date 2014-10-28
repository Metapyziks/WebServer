using System;
using System.Threading;

namespace WebServer
{
    internal class ScheduledJob
    {
        private readonly Action<Server> _job;
        private bool _canceled;

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
            Identifier = ident;

            NextTime = nextTime;
            Interval = interval;

            _job = job;
            _canceled = false;

            Timer = new Timer(Perform, server, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (NextTime < DateTime.Now) {
                Timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            } else {
                var delay = NextTime - DateTime.Now;
                if (delay.TotalMinutes > 1.0) {
                    delay = TimeSpan.FromMinutes(1.0);
                }

                Timer.Change(delay, Timeout.InfiniteTimeSpan);
            }
        }

        private void Perform(Object state)
        {
            if (_canceled) return;
            
            if (!ShouldPerform) {
                UpdateTimer();
                return;
            }

            var server = (Server) state;
            try {
                server.Log("Performing {0}", Identifier);
                _job(server);
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
            _canceled = true;

            NextTime = DateTime.MaxValue;
            Timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            Timer.Dispose();
        }
    }
}

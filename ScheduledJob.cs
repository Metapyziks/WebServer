using System;

namespace WebServer
{
    internal class ScheduledJob
    {
        private readonly Action<Server> _job;
        private bool _canceled;

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

        internal bool Cancelled
        {
            get { return _canceled; }
        }

        internal ScheduledJob(String ident, DateTime nextTime,
            TimeSpan interval, Action<Server> job)
        {
            Identifier = ident;

            NextTime = nextTime;
            Interval = interval;

            _job = job;
            _canceled = false;
        }

        internal void Perform(Object state)
        {
            if (_canceled) return;
            
            if (!ShouldPerform) {
                return;
            }

            NextTime = DateTime.Now + Interval;

            var server = (Server) state;
            try {
#if DEBUG
                server.Log("Performing {0}", Identifier);
#endif
                _job(server);
#if DEBUG
                server.Log("Completed {0}", Identifier);
#endif
            } catch (Exception e) {
                server.Log(e);
            }

            if (OnceOnly) {
                Cancel();
            }
        }

        internal void Cancel()
        {
            _canceled = true;
        }
    }
}

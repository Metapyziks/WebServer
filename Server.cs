﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace WebServer
{
    public class LoggedMessageEventArgs : EventArgs
    {
        public EventLogEntryType Type { get; private set; }
        public String Message { get; private set; }

        public LoggedMessageEventArgs(EventLogEntryType type, String message)
        {
            Message = message;
            Type = type;
        }
    }

    public delegate void LoggedMessageHandler(LoggedMessageEventArgs e);

    public class Server
    {
        private readonly Dictionary<String, Type> _boundServlets;
        private readonly LinkedList<ScheduledJob> _scheduledJobs;

        private readonly AutoResetEvent _scheduledJobHandle;

        private readonly HttpListener _listener;
        private readonly ManualResetEvent _stop;
        private bool _stopped;

        private Type _notFoundServlet;
        private Type _resourceServlet;
        
        public String ResourceRootUrl { get; set; }

        public event LoggedMessageHandler LoggedMessage;
        
        public bool IsListening
        {
            get { return _listener.IsListening; }
        }

        public Server()
        {
            _boundServlets = new Dictionary<String, Type>();
            _scheduledJobs = new LinkedList<ScheduledJob>();

            _scheduledJobHandle = new AutoResetEvent(false);

            _listener = new HttpListener();

            _stop = new ManualResetEvent(false);
            _stopped = false;

            ResourceRootUrl = "/res";

            _notFoundServlet = typeof(Default404Servlet);
            _resourceServlet = typeof(DefaultResourceServlet);

            BindServletToURL<DefaultResourceServlet>("/favicon.ico");
        }

        public void Log(EventLogEntryType type, String format, params object[] args)
        {
            var message = args.Length == 0 ? format : String.Format(format, args);

            if (LoggedMessage != null) {
                LoggedMessage(new LoggedMessageEventArgs(type, message));
            }
        }

        public virtual void Log(Exception e)
        {
            Log(EventLogEntryType.Error, "{0}: {1}", e.Message, e.StackTrace);
        }

        public virtual void Log(String format, params Object[] args)
        {
            Log(EventLogEntryType.Information, format, args);
        }

        public void Stop()
        {
            _stopped = true;
            _stop.Set();

            foreach (var job in _scheduledJobs) {
                job.Cancel();
            }
        }

        public void AddPrefix(String uriPrefix)
        {
            _listener.Prefixes.Add(uriPrefix);
        }

        public void BindServletsInAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(x => typeof(Servlet).IsAssignableFrom(x))
                .Where(x => x.GetCustomAttributes<ServletURLAttribute>().Any());

            foreach (var type in types) {
                BindServletToURL(type);
            }
        }

        public void BindServletToURL<T>()
            where T : Servlet
        {
            BindServletToURL(typeof(T));
        }

        public void BindServletToURL(Type t)
        {
            var attribs = t.GetCustomAttributes<ServletURLAttribute>();

            if (!attribs.Any()) {
                throw new InvalidOperationException("Servlet class must have a ServletURLAttribute");
            }

            foreach (var url in attribs.SelectMany(x => x.URLs)) {
                BindServletToURL(t, url);
            }
        }

        public void BindServletToURL<T>(String url)
            where T : Servlet
        {
            _boundServlets.Add(url, typeof(T));
        }

        public void BindServletToURL(Type t, String url)
        {
            _boundServlets.Add(url, t);
        }

        public void SetNotFoundServlet<T>()
            where T : Servlet
        {
            _notFoundServlet = typeof(T);
        }

        public void SetResourceServlet<T>()
        {
            _resourceServlet = typeof(T);
        }

        public Servlet CreateNotFoundServlet()
        {
            var ctor = _notFoundServlet.GetConstructor(new Type[0]);
            var servlet = (Servlet) ctor.Invoke(new Object[0]);
            servlet.Server = this;
            return servlet;
        }

        public Servlet CreateServlet(String url)
        {
            int queryStart = url.IndexOf('?');
            if (queryStart != -1) {
                url = url.Substring(0, queryStart);
            }

            Type type = ResourceRootUrl == "/" ? _resourceServlet : _notFoundServlet;
            do {
                if (_boundServlets.ContainsKey(url)) {
                    type = _boundServlets[url];
                    break;
                } else if (url == ResourceRootUrl) {
                    type = _resourceServlet;
                    break;
                }

                int divider = url.LastIndexOf('/');
                url = url.Substring(0, divider == -1 ? 0 : divider);
            } while (url.Length > 0);

            var ctor = type.GetConstructor(new Type[0]);
            return (Servlet) ctor.Invoke(new Object[0]);
        }

        public void ScheduleJob(String ident, TimeSpan after, Action<Server> job)
        {
            ScheduleJob(new ScheduledJob(ident, DateTime.Now.Add(after), TimeSpan.Zero, job));
        }

        public void ScheduleJob(String ident, DateTime nextTime, TimeSpan interval, Action<Server> job)
        {
            ScheduleJob(new ScheduledJob(ident, nextTime, interval, job));
        }

        private void ScheduleJob(ScheduledJob job)
        {
            lock (_scheduledJobs) {
                var nextJob = _scheduledJobs.First;
                var first = true;

                while (nextJob != null) {
                    if (nextJob.Value.NextTime <= job.NextTime) {
                        nextJob = nextJob.Next;
                        first = false;
                        continue;
                    }

                    _scheduledJobs.AddBefore(nextJob, job);

                    if (first) _scheduledJobHandle.Set();
                    return;
                }

                _scheduledJobs.AddFirst(job);
                if (first) _scheduledJobHandle.Set();
            }
        }

        private void ScheduledJobTask()
        {
            while (!_stopped) {
                var timeout = 1000;
                var first = _scheduledJobs.FirstOrDefault();

                if (first != null) {
                    timeout = Math.Min(timeout, (int) Math.Ceiling((first.NextTime - DateTime.Now).TotalMilliseconds));
                }

                if (timeout > 0) {
                    _scheduledJobHandle.WaitOne(timeout);
                }

                lock (_scheduledJobs) {
                    first = _scheduledJobs.FirstOrDefault();
                    if (first == null || !first.ShouldPerform) continue;
                    _scheduledJobs.RemoveFirst();
                }

                first.Perform(this);

                if (!first.Cancelled) ScheduleJob(first);
            }
        }
        
        private void HttpRequestTask(Object state)
        {
            if (_stopped) return;
            
            var context = (HttpListenerContext) state;
            try {
                var servlet = CreateServlet(context.Request.RawUrl);
                servlet.Server = this;
                servlet.Service(context.Request, context.Response);
            } catch {
            } finally {
                try {
                    context.Response.Close();
                } catch { }
            }
        }

        public void Run()
        {
            _listener.Start();
            Log("Started Listening");

            new Thread(ScheduledJobTask).Start();

            while (_listener.IsListening && !_stopped) {
                var ctx = _listener.BeginGetContext(res => {
                    try {
                        var context = _listener.EndGetContext(res);
                        ThreadPool.QueueUserWorkItem(HttpRequestTask, context);
                    } catch {
                        return;
                    }
                }, null);

                if (WaitHandle.WaitAny(new[] { _stop, ctx.AsyncWaitHandle }) == 0) {
                    break;
                }
            }

            _listener.Close();
            Log("Stopped Listening");
        }
    }
}

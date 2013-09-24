using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace WebServer
{
    public class Server
    {
        private readonly Dictionary<String, Type> _boundServlets;
        private readonly Dictionary<String, ScheduledJob> _scheduledJobs;

        private readonly HttpListener _listener;
        private readonly ManualResetEvent _stop;
        private bool _stopped;

        private Type _notFoundServlet;
        private Type _resourceServlet;
        
        public String ResourceRootUrl { get; set; }
        
        public bool IsListening
        {
            get { return _listener.IsListening; }
        }

        public Server()
        {
            _boundServlets = new Dictionary<String, Type>();
            _scheduledJobs = new Dictionary<String, ScheduledJob>();

            _listener = new HttpListener();

            _stop = new ManualResetEvent(false);
            _stopped = false;

            ResourceRootUrl = "/res";

            _notFoundServlet = typeof(Default404Servlet);
            _resourceServlet = typeof(DefaultResourceServlet);

            BindServletToURL<DefaultResourceServlet>("/favicon.ico");
        }

        public void Stop()
        {
            _stopped = true;
            _stop.Set();
            foreach (var job in _scheduledJobs) {
                job.Value.Cancel();
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
                .Where(x => x.GetCustomAttributes<ServletURLAttribute>().Count() > 0);

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

            if (attribs.Count() == 0) {
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

            Type type = _notFoundServlet;
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

        public void AddScheduledJob(String ident, DateTime nextTime, TimeSpan interval, Action<Server> job)
        {
            var scheduledJob = new ScheduledJob(this, ident, nextTime, interval, job);
            _scheduledJobs.Add(ident, scheduledJob);
        }

        private void WorkerTask(Object state)
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

            while (_listener.IsListening && !_stopped) {
                var ctx = _listener.BeginGetContext(res => {
                    try {
                        var context = _listener.EndGetContext(res);
                        ThreadPool.QueueUserWorkItem(WorkerTask, context);
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

        public virtual void Log(Exception e)
        {
            Console.WriteLine("[{0}] {1}", DateTime.Now, e);
            Console.WriteLine(e.StackTrace);
        }

        public virtual void Log(String format, params Object[] args)
        {
            Console.WriteLine("[{0}] {1}", DateTime.Now, String.Format(format, args));
        }
    }
}

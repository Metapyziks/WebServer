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
        private readonly Queue<HttpListenerContext> _requestQueue;
        private readonly Dictionary<String, ScheduledJob> _scheduledJobs;

        private readonly HttpListener _listener;

        private readonly int _maxWorkers;
        private readonly Thread[] _workers;
        private Thread _mainThread;

        private readonly ManualResetEvent _ready;
        private readonly ManualResetEvent _stop;

        public String ResourceRootUrl { get; set; }

        public Servlet DefaultServlet { get; set; }
        public DefaultResourceServlet ResourceServlet { get; set; }

        public bool IsListening
        {
            get { return _listener.IsListening; }
        }

        public Server(int maxWorkers = 1)
        {
            _boundServlets = new Dictionary<String, Type>();
            _requestQueue = new Queue<HttpListenerContext>();
            _scheduledJobs = new Dictionary<String, ScheduledJob>();

            _listener = new HttpListener();

            _maxWorkers = Math.Max(1, maxWorkers);
            _workers = new Thread[_maxWorkers];

            _ready = new ManualResetEvent(false);
            _stop = new ManualResetEvent(false);

            ResourceRootUrl = "/res";

            DefaultServlet = new Default404Servlet();
            ResourceServlet = new DefaultResourceServlet();

            BindServletToURL<DefaultResourceServlet>("/favicon.ico");
        }

        public void Stop()
        {
            _stop.Set();
            foreach (var worker in _workers) {
                if (worker != Thread.CurrentThread) worker.Join();
            }
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

        protected Servlet CreateServlet(String url)
        {
            int queryStart = url.IndexOf('?');
            if (queryStart != -1) {
                url = url.Substring(0, queryStart);
            }

            do {
                if (_boundServlets.ContainsKey(url)) {
                    var type = _boundServlets[url];
                    if (type == ResourceServlet.GetType()) {
                        return ResourceServlet;
                    }

                    var ctor = type.GetConstructor(new Type[0]);
                    return (Servlet) ctor.Invoke(new Object[0]);
                } else if (url == ResourceRootUrl) {
                    return ResourceServlet;
                }

                int divider = url.LastIndexOf('/');
                url = url.Substring(0, divider == -1 ? 0 : divider);
            } while (url.Length > 0);

            return DefaultServlet;
        }

        public void AddScheduledJob(String ident, DateTime nextTime, TimeSpan interval, Action<Server> job)
        {
            var scheduledJob = new ScheduledJob(this, ident, nextTime, interval, job);
            _scheduledJobs.Add(ident, scheduledJob);
        }

        private void WorkerLoop()
        {
            var waitHandles = new[] { _ready, _stop };
            while (WaitHandle.WaitAny(waitHandles) == 0) {
                HttpListenerContext context;
                lock (_requestQueue) {
                    if (_requestQueue.Count > 0) {
                        context = _requestQueue.Dequeue();
                    } else {
                        _ready.Reset();
                        return;
                    }
                }

                try {
                    var servlet = CreateServlet(context.Request.RawUrl);
                    servlet.Server = this;
                    servlet.Service(context.Request, context.Response);
                } finally {
                    if (context != null) {
                        context.Response.Close();
                    }
                }        
            }
        }

        public void Run()
        {
            _mainThread = Thread.CurrentThread;

            _listener.Start();

            for (int i = 0; i < _maxWorkers; ++i) {
                _workers[i] = new Thread(WorkerLoop);
                _workers[i].Start();
            }

            while (_listener.IsListening) {
                var ctx = _listener.BeginGetContext(res => {
                    try {
                        lock (_requestQueue) {
                            _requestQueue.Enqueue(_listener.EndGetContext(res));
                            _ready.Set();
                        }
                    } catch {
                        return;
                    }
                }, null);

                if (WaitHandle.WaitAny(new[] { _stop, ctx.AsyncWaitHandle }) == 0) {
                    break;
                }
            }

            _listener.Close();
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

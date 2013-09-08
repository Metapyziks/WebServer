using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace WebServer
{
    public class Server
    {
        private Dictionary<String, Type> _boundServlets;
        private HttpListener _listener;

        public String ResourceRootUrl { get; set; }

        public Servlet DefaultServlet { get; set; }
        public DefaultResourceServlet ResourceServlet { get; set; }

        public bool IsListening
        {
            get { return _listener.IsListening; }
        }

        public Server()
        {
            _boundServlets = new Dictionary<String, Type>();
            _listener = new HttpListener();

            ResourceRootUrl = "/res";

            DefaultServlet = new Default404Servlet();
            ResourceServlet = new DefaultResourceServlet();
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

        public void Run()
        {
            _listener.Start();

            while (_listener.IsListening) {
                HttpListenerContext context = null;
                try {
                    context = _listener.GetContext();
                    var servlet = CreateServlet(context.Request.RawUrl);
                    servlet.Server = this;
                    servlet.Service(context.Request, context.Response);
                } catch {
                    if (context != null) {
                        context.Response.Close();
                    }
                }
            }

            _listener.Close();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;

namespace WebServer
{
    public class Server
    {
        private Dictionary<String, Type> _boundServlets;
        private HttpListener _listener;

        public Servlet DefaultServlet { get; set; }

        public bool IsListening
        {
            get { return _listener.IsListening; }
        }

        public Server()
        {
            _boundServlets = new Dictionary<String, Type>();
            _listener = new HttpListener();

            DefaultServlet = new Default404Servlet();
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
                }

                int divider = url.LastIndexOf('/');
                url = url.Substring(0, divider);
            } while (url.Length > 0);

            return DefaultServlet;
        }

        public void Run()
        {
            _listener.Start();

            while (_listener.IsListening) {
                var context = _listener.GetContext();
                var servlet = CreateServlet(context.Request.RawUrl);

                servlet.Service(context.Request, context.Response);
            }

            _listener.Close();
        }
    }
}

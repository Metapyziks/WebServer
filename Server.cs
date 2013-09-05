using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace WebServer
{
    public class Server
    {
        private Dictionary<String, Type> _boundServlets;

        public Server()
        {
            _boundServlets = new Dictionary<String, Type>();
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
    }
}

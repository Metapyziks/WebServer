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

        public void BindServletToURL<T>()
            where T : Servlet
        {
            var attribs = typeof(T).GetCustomAttributes<ServletURLAttribute>();

            if (attribs.Count() == 0) {
                throw new InvalidOperationException("Servlet class must have a ServletURLAttribute");
            }

            foreach (var url in attribs.SelectMany(x => x.URLs)) {
                BindServletToURL<T>(url);
            }
        }

        public void BindServletToURL<T>(String url)
            where T : Servlet
        {
            _boundServlets.Add(url, typeof(T));
        }
    }
}

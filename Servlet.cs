using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServletURLAttribute : Attribute
    {
        public String[] URLs { get; private set; }

        public ServletURLAttribute(params String[] urls)
        {
            URLs = urls;
        }
    }

    public abstract class Servlet
    {
        protected HttpListenerRequest Request { get; private set; }
        protected HttpListenerResponse Response { get; private set; }

        protected StreamWriter Writer { get; private set; }

        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;

            using (Writer = new StreamWriter(Response.OutputStream)) {
                OnService();
            }
        }

        public String Tag(String name)
        {
            return String.Format("<{0} />", name);
        }

        public String Tag(String name, Func<String> body)
        {
            return String.Format("<{0}>{1}</{0}>", name, body());
        }

        public String NewLine()
        {
            return Tag("br");
        }

        protected abstract void OnService();
    }
}

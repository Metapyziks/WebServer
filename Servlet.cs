using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        public String Ln
        {
            get { return Tag("br"); }
        }

        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;

            using (Writer = new StreamWriter(Response.OutputStream)) {
                OnService();
            }
        }

        public String Format(String format, params object[] args)
        {
            return String.Format(format, args);
        }

        public Dictionary<String, Object> Attribs(params Expression<Func<String, Object>>[] args)
        {
            var dict = new Dictionary<String, Object>();
            foreach (var arg in args) {
                dict.Add(arg.Parameters.First().Name, arg.Compile()(String.Empty));
            }
            return dict;
        }

        public String Tag(String name)
        {
            return String.Format("<{0} />", name);
        }

        public String Tag(String name, params String[] body)
        {
            return String.Format("<{0}>{1}</{0}>", name, String.Join(String.Empty, body));
        }

        public String Tag(String name, Dictionary<String, Object> attribs, params String[] body)
        {
            var attribStrings = attribs.Select(kv => String.Format(" {0}=\"{1}\"", kv.Key, kv.Value));
            var attribsJoined = String.Join(String.Empty, attribStrings);

            return String.Format("<{0}{1}>{2}</{0}>", name, attribsJoined, String.Join(String.Empty, body));
        }

        protected abstract void OnService();
    }
}

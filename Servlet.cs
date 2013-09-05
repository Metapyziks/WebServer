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

        protected Func<String, String, String> Attrib { get; private set; }

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

        public String Tag(String name)
        {
            return String.Format("<{0} />", name);
        }

        public String Tag(String name, String body)
        {
            return String.Format("<{0}>{1}</{0}>", name, body);
        }

        public String Tag(String name, Func<String> body)
        {
            var attribDict = new Dictionary<String, String>();
            var oldAttrib = Attrib;

            Attrib = (key, value) => { attribDict.Add(key, value); return String.Empty; };
            var contents = body();
            Attrib = oldAttrib;

            var attribStrings = attribDict.Select(kv => String.Format(" {0}=\"{1}\"", kv.Key, kv.Value));
            var attribs = String.Join(String.Empty, attribStrings);

            return String.Format("<{0}{1}>{2}</{0}>", name, attribs, contents);
        }

        protected abstract void OnService();
    }
}

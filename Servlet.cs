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
            get { return EmptyTag("br"); }
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

        public delegate String TagDelegate(params String[] body);

        private String JoinAttributes(Expression<Func<String, Object>>[] attributes)
        {
            var attribStrings = attributes.Select(attrib => String.Format(" {0}=\"{1}\"",
                attrib.Parameters.First().Name, attrib.Compile()(String.Empty)));
            return String.Join(String.Empty, attribStrings);
        }

        public String EmptyTag(String name, params Expression<Func<String, Object>>[] attributes)
        {
            var attribsJoined = JoinAttributes(attributes);
            return String.Format("<{0}{1} />", name, attribsJoined);
        }

        public TagDelegate Tag(String name, params Expression<Func<String, Object>>[] attributes)
        {
            var attribsJoined = JoinAttributes(attributes);
            return (body) => String.Format("<{0}{1}>{2}</{0}>",
                name, attribsJoined, String.Join(String.Empty, body));
        }

        protected abstract void OnService();
    }
}

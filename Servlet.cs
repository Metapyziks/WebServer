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
        public delegate void WriteDelegate(params Object[] body);
        public delegate String BodyDelegate(params Object[] body);

        protected HttpListenerRequest Request { get; private set; }
        protected HttpListenerResponse Response { get; private set; }

        private StreamWriter _streamWriter;

        protected WriteDelegate Write { get; private set; }

        public String Ln
        {
            get { return EmptyTag("br"); }
        }

        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;

            using (_streamWriter = new StreamWriter(Response.OutputStream)) {
                Write = x => { foreach (var str in x) _streamWriter.Write(str); };
                OnService();
            }
        }

        public String Format(String format, params object[] args)
        {
            return String.Format(format, args);
        }


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

        public BodyDelegate Tag(String name, params Expression<Func<String, Object>>[] attributes)
        {
            var attribsJoined = JoinAttributes(attributes);
            return (body) => String.Format("<{0}{1}>{2}</{0}>",
                name, attribsJoined, String.Join(String.Empty, body));
        }

        public String Dynamic(Action body)
        {
            var oldWrite = Write;
            var sb = new StringBuilder();
            Write = x => { foreach (var str in x) sb.Append(str); };
            body();
            Write = oldWrite;
            return sb.ToString();
        }

        protected abstract void OnService();
    }
}

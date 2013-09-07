using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;

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
        protected static String[] SplitURL(String url)
        {
            if (url.StartsWith("/")) {
                url = url.Substring(1);
            }

            int queryStart = url.IndexOf('?');
            if (queryStart != -1) {
                url = url.Substring(0, queryStart);
            }

            return url.Split('/');
        }

        protected static String URLRelativeTo(String url, String root)
        {
            int length = root.EndsWith("/") ? root.Length : root.Length + 1;
            return url.Substring(length);
        }

        public delegate void WriteDelegate(params Object[] body);
        public delegate String BodyDelegate(params Object[] body);

        public Server Server { get; internal set; }

        protected String[] URLParts { get; private set; }

        protected HttpListenerRequest Request { get; private set; }
        protected HttpListenerResponse Response { get; private set; }

        private StreamWriter _streamWriter;
        private int _indentDepth;

        protected WriteDelegate Write { get; private set; }

        protected String Ln
        {
            get { return EmptyTag("br"); }
        }

        protected String Indent(int depth)
        {
            var spaces = Enumerable.Range(0, _indentDepth * 2).Select(x => ' ');
            return String.Join(String.Empty, spaces);
        }

        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;

            Response.ContentType = "text/html";

            using (_streamWriter = new StreamWriter(Response.OutputStream)) {
                Write = x => {
                    foreach (var str in x) {
                        _streamWriter.WriteLine("{0}{1}", Indent(_indentDepth), str);
                    }
                };
                try {
                    OnService();
                } catch (Exception e) {
                    Console.WriteLine(e);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private String JoinAttributes(Expression<Func<String, Object>>[] attributes)
        {
            var attribStrings = attributes.Select(attrib => String.Format(" {0}=\"{1}\"",
                attrib.Parameters.First().Name, attrib.Compile()(String.Empty)));
            return String.Join(String.Empty, attribStrings);
        }

        protected String Format(String format, params object[] args)
        {
            return String.Format(format, args);
        }

        protected String EmptyTag(String name, params Expression<Func<String, Object>>[] attributes)
        {
            var attribsJoined = JoinAttributes(attributes);
            return String.Format("<{0}{1} />", name, attribsJoined);
        }

        protected BodyDelegate Tag(String name, params Expression<Func<String, Object>>[] attributes)
        {
            var attribsJoined = JoinAttributes(attributes);
            ++_indentDepth;
            return (body) => {
                var bodyJoined = String.Join(String.Empty, body.Select(x =>
                    String.Format("{0}{1}{2}", Indent(_indentDepth), x, Environment.NewLine)));
                --_indentDepth;
                return String.Format("<{0}{1}>{3}{2}{4}</{0}>", name, attribsJoined, bodyJoined,
                    Environment.NewLine, Indent(_indentDepth));
            };
        }

        protected String DocType(params String[] args)
        {
            return String.Format("<!DOCTYPE {0}>", String.Join(" ", args));
        }

        protected String Dynamic(Action body)
        {
            var sb = new StringBuilder();
            var oldWrite = Write;
            Write = x => {
                bool first = true;
                foreach (var str in x) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(Environment.NewLine);
                        sb.Append(Indent(_indentDepth));
                    }
                    sb.Append(str.ToString());
                }
            };
            body();
            Write = oldWrite;
            return sb.ToString();
        }

        protected abstract void OnService();
    }
}

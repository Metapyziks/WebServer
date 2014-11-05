using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;

namespace WebServer
{
    public abstract class HTMLServlet : Servlet
    {
        public delegate void WriteDelegate(params Object[] body);
        public delegate String BodyDelegate(params Object[] body);

        private StreamWriter _streamWriter;

        protected WriteDelegate Write { get; private set; }

        protected String Ln
        {
            get { return EmptyTag("br"); }
        }

        protected override bool OnPreService()
        {
            Response.ContentType = "text/html";

            _streamWriter = new StreamWriter(Response.OutputStream);

            Write = x => {
                foreach (var str in x) {
                    _streamWriter.WriteLine(str);
                }
            };

            return true;
        }

        protected override void OnPostService()
        {
            _streamWriter.Flush();
        }

        private String JoinAttributes(Expression<Func<String, Object>>[] attributes)
        {
            var attribStrings = attributes.Select(attrib => {
                var key = attrib.Parameters.First().Name;
                var value = attrib.Compile()(String.Empty);
                if (value == null) return String.Empty;
                if (value is bool) value = value.ToString().ToLower();
                return String.Format(" {0}=\"{1}\"", key, value);
            });
            return String.Join(String.Empty, attribStrings);
        }

        protected String Escape(String str)
        {
            return HttpUtility.HtmlEncode(str);
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
            return (body) => {
                var bodyJoined = String.Join(String.Empty, body.Select(x =>
                    x is BodyDelegate ? ((BodyDelegate) x)() : x));
                return String.Format("<{0}{1}>{2}</{0}>", name, attribsJoined, bodyJoined);
            };
        }

        protected String DocType(params String[] args)
        {
            return Format("<!DOCTYPE {0}>{1}", String.Join(" ", args), Environment.NewLine);
        }

        protected String Dyn(Action body)
        {
            var sb = new StringBuilder();
            var oldWrite = Write;
            Write = x => {
                foreach (var str in x) {
                    sb.Append(str is BodyDelegate ? ((BodyDelegate) str)() : str.ToString());
                }
            };
            body();
            Write = oldWrite;
            return sb.ToString();
        }
    }
}

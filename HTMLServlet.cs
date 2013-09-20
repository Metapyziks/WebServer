using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace WebServer
{
    public abstract class HTMLServlet : Servlet
    {
        public delegate void WriteDelegate(params Object[] body);
        public delegate String BodyDelegate(params Object[] body);

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

        protected override bool OnPreService()
        {
            Response.ContentType = "text/html";

            _streamWriter = new StreamWriter(Response.OutputStream);

            Write = x => {
                foreach (var str in x) {
                    _streamWriter.WriteLine("{0}{1}", Indent(_indentDepth), str);
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
                if (value is bool) value = value.ToString().ToLower();
                return String.Format(" {0}=\"{1}\"", key, value);
            });
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
                    String.Format("{0}{1}{2}", Indent(_indentDepth),
                    x is BodyDelegate ? ((BodyDelegate) x)() : x, Environment.NewLine)));
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
                    sb.Append(str is BodyDelegate ? ((BodyDelegate) str)() : str.ToString());
                }
            };
            body();
            Write = oldWrite;
            return sb.ToString();
        }
    }
}

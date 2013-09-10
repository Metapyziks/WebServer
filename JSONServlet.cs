using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public abstract class JSONServlet : Servlet
    {
        public delegate void WriteDelegate(params Object[] body);
        public delegate String BodyDelegate(params Object[] body);

        private StreamWriter _streamWriter;
        private int _indentDepth;

        protected WriteDelegate Write { get; private set; }

        protected String Indent(int depth)
        {
            var spaces = Enumerable.Range(0, _indentDepth * 2).Select(x => ' ');
            return String.Join(String.Empty, spaces);
        }

        protected override void OnPreService()
        {
            Response.ContentType = "application/json";

            _streamWriter = new StreamWriter(Response.OutputStream);

            Write = x => {
                var str = String.Join(",", x.Select(y => Format("{0}{1}", Indent(_indentDepth), y)));
                _streamWriter.WriteLine(str);
            };
        }

        protected override void OnPostService()
        {
            _streamWriter.Flush();
        }

        protected String Format(String format, params object[] args)
        {
            return String.Format(format, args);
        }

        protected String Pair(String key, String value)
        {
            return Format("\"{0}\" : {1}", key, value);
        }

        protected String Str(object str)
        {
            return Format("\"{0}\"", str);
        }

        protected BodyDelegate Object
        {
            get {
                ++_indentDepth;
                return (body) => {
                    var bodyJoined = String.Join("," + Environment.NewLine, body.Select(x =>
                        String.Format("{0}{1}", Indent(_indentDepth), x))) + Environment.NewLine;
                    --_indentDepth;
                    return "{" + Environment.NewLine + bodyJoined + Indent(_indentDepth) + "}";
                };
            }
        }

        protected BodyDelegate Array
        {
            get
            {
                ++_indentDepth;
                return (body) => {
                    var bodyJoined = String.Join("," + Environment.NewLine, body.Select(x =>
                        String.Format("{0}{1}", Indent(_indentDepth), x))) + Environment.NewLine;
                    --_indentDepth;
                    return "[" + Environment.NewLine + bodyJoined + Indent(_indentDepth) + "]";
                };
            }
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
    }
}

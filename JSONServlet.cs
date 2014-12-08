using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WebServer
{
    public abstract class JSONServlet : Servlet
    {
        public delegate void WriteDelegate(params Object[] body);
        public delegate String BodyDelegate(params Object[] body);

        private StreamWriter _streamWriter;

        protected WriteDelegate Write { get; private set; }

        protected override bool OnPreService()
        {
            Response.ContentType = "application/json";

            _streamWriter = new StreamWriter(Response.OutputStream);

            Write = x => {
                var str = String.Join(",", x);
                _streamWriter.WriteLine(str);
            };

            return true;
        }

        protected override void OnPostService()
        {
            _streamWriter.Flush();
        }

        protected String Error(String message, bool shouldRetry = false)
        {
            return Object(Pair("success", false), Pair("error", Str(message)), Pair("retry", shouldRetry));
        }

        protected String Success(params Object[] body)
        {
            return Object(new Object[] { Pair("success", true) }.Concat(body).ToArray() );
        }

        protected String Format(String format, params object[] args)
        {
            return String.Format(format, args);
        }

        protected String Pair(String key, Object value)
        {
            return Format("\"{0}\":{1}", key, value is bool ? value.ToString().ToLower() : (value ?? "null"));
        }

        protected String Str(object str)
        {
            return Format("\"{0}\"", str.ToString().Replace("\\", "\\\\").Replace("\"", "\\\""));
        }

        protected BodyDelegate Object
        {
            get {
                return body => "{" + String.Join(",", body) + "}";
            }
        }

        protected BodyDelegate Array
        {
            get
            {
                return body => "[" + String.Join(",", body) + "]";
            }
        }

        protected String Dyn(Action body)
        {
            var sb = new StringBuilder();
            var oldWrite = Write;
            var first = true;
            Write = x => {
                foreach (var str in x) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(",");
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

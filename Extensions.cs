using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace WebServer
{
    public static class Extensions
    {
        public static String GetBodyString(this HttpListenerRequest request, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;

            var pos = request.InputStream.Position;

            request.InputStream.Seek(0, SeekOrigin.Begin);
            var str = new StreamReader(request.InputStream, encoding).ReadToEnd();
            request.InputStream.Seek(pos, SeekOrigin.Begin);

            return str;
        }

        public static NameValueCollection GetParsedBody(this HttpListenerRequest request)
        {
            return HttpUtility.ParseQueryString(request.GetBodyString());
        }

        public static MultipartFormField GetMultipartForm(this HttpListenerRequest request)
        {
            if (request.ContentType == null) throw new NullReferenceException("Request content type was null.");

            var headers = request.Headers.AllKeys.ToDictionary(
                x => x, x => FormFieldHeader.Parse(request.Headers[x]));

            var pos = request.InputStream.Position;

            request.InputStream.Seek(0, SeekOrigin.Begin);
            var fields = new MultipartFormField(headers, request.InputStream);
            request.InputStream.Seek(pos, SeekOrigin.Begin);

            return fields;
        }
    }
}

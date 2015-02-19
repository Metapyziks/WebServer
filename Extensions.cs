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
        public static String[] GetUrlSegments(this HttpListenerRequest request)
        {
            return request.Url.Segments
                .Select(x => x.TrimEnd('/'))
                .Where(x => x.Length > 0)
                .ToArray();
        }

        public static String ReadBodyString(this HttpListenerRequest request, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;

            using (var reader = new StreamReader(request.InputStream, encoding)) {
                return reader.ReadToEnd();
            }
        }

        public static NameValueCollection ReadParsedBody(this HttpListenerRequest request)
        {
            return HttpUtility.ParseQueryString(request.ReadBodyString());
        }

        public static MultipartFormField ReadMultipartForm(this HttpListenerRequest request)
        {
            if (request.ContentType == null) throw new NullReferenceException("Request content type was null.");

            var headers = request.Headers.AllKeys.ToDictionary(
                x => x, x => FormFieldHeader.Parse(request.Headers[x]));

            using (var copy = new MemoryStream()) {
                request.InputStream.CopyTo(copy);
                copy.Seek(0, SeekOrigin.Begin);
                return new MultipartFormField(new FormField.HeaderCollection(headers), copy);
            }
        }

        public static ulong ToUnixTimestamp(this DateTime time)
        {
            return (ulong) (time - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}

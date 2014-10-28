using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace WebServer
{
    public static class Extensions
    {
        public static NameValueCollection GetParsedPost(this HttpListenerRequest request)
        {
            var body = new StreamReader(request.InputStream).ReadToEnd();
            var pairs = body.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var output = new NameValueCollection();

            foreach (var pair in pairs) {
                var nv = pair.Split('=');
                if (nv.Length == 2) {
                    output.Add(nv[0], WebUtility.UrlDecode(nv[1]));
                } else {
                    output.Add(nv[0], String.Empty);
                }
            }

            return output;
        }
    }
}

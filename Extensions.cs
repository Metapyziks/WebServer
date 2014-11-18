using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace WebServer
{
    public static class Extensions
    {
        public static String GetBodyString(this HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream)) {
                return reader.ReadToEnd();
            }
        }

        public static NameValueCollection GetParsedBody(this HttpListenerRequest request)
        {
            return HttpUtility.ParseQueryString(request.GetBodyString());
        }
    }
}

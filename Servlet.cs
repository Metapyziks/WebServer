using System;
using System.Net;

namespace WebServer
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServletUrlAttribute : Attribute
    {
        public String[] Urls { get; private set; }

        public ServletUrlAttribute(params String[] urls)
        {
            Urls = urls;
        }
    }

    public abstract class Servlet
    {
        protected static String[] SplitUrl(String url)
        {
            if (url.Length == 0) return new String[0];

            if (url.StartsWith("/")) {
                url = url.Substring(1);
            }

            int queryStart = url.IndexOf('?');
            if (queryStart != -1) {
                url = url.Substring(0, queryStart);
            }

            return url.Split('/');
        }

        protected static String UrlRelativeTo(String url, String root)
        {
            int length = root.EndsWith("/") || url.Length <= root.Length ? root.Length : root.Length + 1;
            return url.Substring(length);
        }

        public Server Server { get; internal set; }

        protected HttpListenerRequest Request { get; private set; }
        protected HttpListenerResponse Response { get; private set; }

        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;

            try {
                if (OnPreService()) {
                    OnService();
                    OnPostService();
                } else if (Response.StatusCode == 200) {
                    Server.CreateErrorServlet().Service(request, response);
                }
            } catch (Exception e) {
                Server.Log(e);
            } finally {
                try {
                    Response.Close();
                } catch { }
            }
        }

        protected virtual bool OnPreService() { return true; }

        protected virtual void OnService() { }

        protected virtual void OnPostService() { }
    }
}

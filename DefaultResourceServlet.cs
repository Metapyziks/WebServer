using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace WebServer
{
    public class DefaultResourceServlet : Servlet
    {
        public static readonly Dictionary<String, String> ContentTypes = new Dictionary<string,string>() {
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".xml", "application/xml" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".ttf", "font/ttf" },
            { ".ico", "image/x-icon" },
            { ".unity3d", "application/vnd.unity" },
            { ".txt", "text/plain" },
            { ".log", "text/plain" },
            { ".zip", "application/zip" },
            { ".gz", "application/x-gzip" },
            { ".mp4", "video/mp4" }
        };

        public static DateTime VersionDate { get; private set; }
        public static String VersionNonce { get; private set; }

        static DefaultResourceServlet()
        {
            VersionDate = DateTime.Now;

            var nonce = BitConverter.GetBytes(VersionDate.Ticks);
            VersionNonce = String.Join(String.Empty, nonce.Select(x => x.ToString("x2")));
        }

        public static String ResourceDirectory { get; set; }
        public static bool EnableCaching { get; set; }

        protected override void OnService()
        {
            if (ResourceDirectory != null) {
                if (EnableCaching) {
                    var etag = Request.Headers["If-None-Match"];
                    if (etag != null && etag.Equals(VersionNonce)) {
                        Response.StatusCode = 304;
                        return;
                    }
                }

                var url = Request.Url.LocalPath;
                if (url.StartsWith(Server.ResourceRootUrl)) {
                    url = UrlRelativeTo(url, Server.ResourceRootUrl);
                }

                var path = Path.GetFullPath(ResourceDirectory + "/" + url);
                var ext = Path.GetExtension(path);

                if (ContentTypes.ContainsKey(ext) && File.Exists(path)) {
                    Response.ContentType = ContentTypes[ext];

                    if (EnableCaching) {
                        Response.AddHeader("ETag", VersionNonce);
                    }

                    if (Request.HttpMethod != "HEAD") {
                        using (var stream = File.OpenRead(path)) {
                            Response.ContentLength64 = stream.Length;

                            stream.CopyTo(Response.OutputStream);
                            stream.Flush();
                        }
                    } else {
                        var info = new FileInfo(path);
                        Response.ContentLength64 = info.Length;
                    }
                    return;
                }
            }

            Server.CreateErrorServlet().Service(Request, Response);
        }
    }
}

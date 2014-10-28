using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace WebServer
{
    public class DefaultResourceServlet : Servlet
    {
        private static readonly Dictionary<String, String> _sContentTypes = new Dictionary<string,string>() {
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".xml", "application/xml" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".ttf", "font/ttf" },
            { ".ico", "image/x-icon" },
            { ".unity3d", "application/vnd.unity" }
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
                    url = URLRelativeTo(url, Server.ResourceRootUrl);
                }

                var path = Path.GetFullPath(ResourceDirectory + "/" + url);
                var ext = Path.GetExtension(path);

                if (_sContentTypes.ContainsKey(ext) && File.Exists(path)) {
                    Response.ContentType = _sContentTypes[ext];

                    if (EnableCaching) {
                        Response.AddHeader("ETag", VersionNonce);
                    }

                    using (var stream = File.OpenRead(path)) {
                        stream.CopyTo(Response.OutputStream);
                        stream.Flush();
                    }
                    return;
                }
            }

            Server.CreateNotFoundServlet().Service(Request, Response);
        }
    }
}

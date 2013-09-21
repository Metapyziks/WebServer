using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace WebServer
{
    public class DefaultResourceServlet : Servlet
    {
        private static readonly Dictionary<String, String> _sContentTypes = new Dictionary<string,string>() {
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".ttf", "font/ttf" },
            { ".ico", "image/x-icon" }
        };

        private static readonly String _sETag;
        private static readonly DateTime _sModifyDate;

        static DefaultResourceServlet()
        {
            _sModifyDate = DateTime.Now;
            var nonce = _sModifyDate.ToString() + Assembly.GetExecutingAssembly().GetName().Version;
            var hashAlg = SHA256.Create();
            var hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(nonce));

            _sETag = String.Join(String.Empty, hash.Select(x => x.ToString("x2")));
        }

        public static String ResourceDirectory { get; set; }
        public static bool EnableCaching { get; set; }

        protected override void OnService()
        {
            if (ResourceDirectory != null) {
                if (EnableCaching) {
                    var etag = Request.Headers["If-None-Match"];
                    if (etag != null && etag.Equals(_sETag)) {
                        Response.StatusCode = 304;
                        return;
                    }
                }

                var url = Request.RawUrl;
                if (url.StartsWith(Server.ResourceRootUrl)) {
                    url = URLRelativeTo(url, Server.ResourceRootUrl);
                }

                var path = Path.GetFullPath(ResourceDirectory + "/" + url);
                var ext = Path.GetExtension(path);

                if (_sContentTypes.ContainsKey(ext) && File.Exists(path)) {
                    Response.ContentType = _sContentTypes[ext];

                    if (EnableCaching) {
                        Response.AddHeader("ETag", _sETag);
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

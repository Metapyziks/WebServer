using System;
using System.Collections.Generic;
using System.IO;

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

        public String ResourceDirectory { get; set; }
        public bool EnableCaching { get; set; }

        protected override void OnService()
        {
            if (ResourceDirectory != null) {
                var url = Request.RawUrl;
                if (url.StartsWith(Server.ResourceRootUrl)) {
                    url = URLRelativeTo(url, Server.ResourceRootUrl);
                }

                var path = Path.GetFullPath(ResourceDirectory + "/" + url);
                var ext = Path.GetExtension(path);

                if (_sContentTypes.ContainsKey(ext) && File.Exists(path)) {
                    Response.ContentType = _sContentTypes[ext];

                    if (EnableCaching) {
                        Response.AddHeader("Cache-Control", "max-age=290304000, public");
                    }

                    using (var stream = File.OpenRead(path)) {
                        stream.CopyTo(Response.OutputStream);
                        stream.Flush();
                    }
                    return;
                }
            }

            Server.DefaultServlet.Service(Request, Response);
        }
    }
}

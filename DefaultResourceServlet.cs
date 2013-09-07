using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            { ".ttf", "font/ttf" }
        };

        public String ResourceDirectory { get; set; }
        public bool EnableCaching { get; set; }

        protected override void OnService()
        {
            if (ResourceDirectory != null) {
                var url = URLRelativeTo(Request.RawUrl, Server.ResourceRootUrl);
                var path = Path.GetFullPath(ResourceDirectory + "/" + url);
                Console.WriteLine(path);
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

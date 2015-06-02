using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace WebServer
{
    public class DefaultResourceServlet : Servlet
    {
        private const int DefaultCopyBufferSize = 2048;
        
        [ThreadStatic]
        private static byte[] _sDefaultCopyBuffer;
        
        private static void CopyTo(Stream from, Stream dest, int length, byte[] buffer = null)
        {
            buffer = buffer ?? _sDefaultCopyBuffer ?? (_sDefaultCopyBuffer = new byte[DefaultCopyBufferSize]);
            var bufferSize = buffer.Length;

            int toRead, read, total = 0;
            while ((toRead = Math.Min(length - total, bufferSize)) > 0
                && (read = from.Read(buffer, 0, toRead)) > 0) {
                dest.Write(buffer, 0, read);
                total += read;
            }
        }

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
            Response.Headers.Add("Accept-Ranges", "bytes");

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

                    int min = 0, max = int.MaxValue;

                    var rangeHeader = Request.Headers["Range"];
                    if (rangeHeader != null && rangeHeader.StartsWith("bytes=")) {
                        rangeHeader = rangeHeader.Substring("bytes=".Length);
                        var split = rangeHeader.Split('-');
                        if (split.Length == 2) {
                            if (split[0].Length > 0) int.TryParse(split[0], out min);
                            if (split[1].Length > 0) int.TryParse(split[1], out max);
                        }
                    }

                    if (Request.HttpMethod != "HEAD") {
                        using (var stream = File.OpenRead(path)) {
                            max = Math.Min(max, (int) stream.Length);

                            Response.ContentLength64 = max - min;

                            if (min != 0 || max != stream.Length) {
                                Response.StatusCode = 206;
                                Response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", min, max, stream.Length));
                            }

                            stream.Seek(min, SeekOrigin.Begin);
                            CopyTo(stream, Response.OutputStream, max - min);
                            stream.Flush();
                        }
                    } else {
                        var info = new FileInfo(path);
                        max = Math.Min(max, (int) info.Length);
                        Response.ContentLength64 = max - min;
                    }
                    return;
                }
            }

            Server.CreateErrorServlet().Service(Request, Response);
        }
    }
}

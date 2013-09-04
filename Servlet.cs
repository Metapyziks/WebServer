using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public abstract class Servlet
    {
        protected HttpListenerRequest Request { get; private set; }
        protected HttpListenerResponse Response { get; private set; }

        protected StreamWriter Writer { get; private set; }

        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;

            using (Writer = new StreamWriter(Response.OutputStream)) {
                OnService();
            }
        }

        protected abstract void OnService();
    }
}

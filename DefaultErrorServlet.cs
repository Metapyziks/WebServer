
using System;
using System.Collections.Generic;

namespace WebServer
{
    public class DefaultErrorServlet : HTMLServlet
    {
        private static readonly Dictionary<int, String> _sErrorCodes = new Dictionary<int, string> {
            { 400, "The URL you requested could not be understood."},
            { 401, "You are not authorized to access the requested URL."},
            { 403, "You are forbidden from accessing the requested URL."},
            { 404, "The URL you requested could not be found."},
        };

        protected override void OnService()
        {
            if (Response.StatusCode < 400) {
                Response.StatusCode = 404;
            }

            Write(
                DocType("html"),
                T("html", lang => "en", another => "blah")(
                    T("head")(
                        T("title")(F("Error {0}", Response.StatusCode))
                    ),
                    T("body")(
                        T("p")(
                            _sErrorCodes.ContainsKey(Response.StatusCode) ? _sErrorCodes[Response.StatusCode] : "An error has occurred.", Ln,
                            T("code")(Request.RawUrl)
                        )
                    )
                )
            );
        }
    }
}

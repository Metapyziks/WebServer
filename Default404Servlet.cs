using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public class Default404Servlet : Servlet
    {
        protected override void OnService()
        {
            Write(
                DocType("html"),
                Tag("html", lang => "en", another => "blah")(
                    Tag("head")(
                        Tag("title")("Error 404")
                    ),
                    Tag("body")(
                        Tag("p")(
                            "The URL you requested could not be found", Ln,
                            Tag("code")(Request.RawUrl)
                        )
                    )
                )
            );
        }
    }
}

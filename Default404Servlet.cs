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
            Writer.Write(
                Tag("html", () =>
                    Attrib("lang", "en") +
                    Tag("head",
                        Tag("title", "Error 404")
                    ) +
                    Tag("body",
                        Tag("p",
                            "The URL you requested could not be found" + Ln +
                            Tag("code", Request.RawUrl)
                        )
                    )
                )
            );
        }
    }
}

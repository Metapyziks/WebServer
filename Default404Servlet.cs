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
                    Tag("head", () =>
                        Tag("title", () => "Error 404")
                    ) +
                    Tag("body", () =>
                        "The URL you requested could not be found"
                    )
                )
            );
        }
    }
}

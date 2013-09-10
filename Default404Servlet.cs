
namespace WebServer
{
    public class Default404Servlet : HTMLServlet
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

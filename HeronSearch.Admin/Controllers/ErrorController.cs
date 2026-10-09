using System.Globalization;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeronSearch.Admin.Controllers;

// Shows the error pages. The status code pages and the exception handler re-run the failed request here with its
// original HTTP method, so this controller allows signed-out visitors, skips the anti-forgery check (a POST with a bad
// token must still get its Bad Request page), and accepts every HTTP method. No page shows technical details.
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class ErrorController : Controller
{
    // Shows the page for the code in /Error/{code} and keeps the code as the response status: 400 Bad Request,
    // 403 Access Denied, 404 Not Found (with a Dashboard link), and the generic error page for any other error code.
    // The route has no HTTP-verb attribute, so every method matches. Error pages are never cached.
    [Route("Error/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Show()
    {
        // Read the code from the route data instead of an action parameter: parameter binding also reads the form of
        // a re-executed POST, and an unreadable form (such as an upload over the size limit) would leave the code at 0.
        var routeValue = RouteData.Values["code"]?.ToString();
        if (!int.TryParse(routeValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)
            || code < 400 || code > 599)
        {
            // A code outside 400-599, such as /Error/200, is not an error page, so it gets Not Found like any unknown URL.
            code = StatusCodes.Status404NotFound;
        }

        var result = code switch
        {
            StatusCodes.Status400BadRequest => View("BadRequest"),
            StatusCodes.Status403Forbidden => View("AccessDenied"),
            StatusCodes.Status404NotFound => View("NotFound", new NotFoundViewModel { ListName = "Dashboard", ListUrl = Url.Content("~/") }),
            _ => View("Error")
        };

        // The view result sets the response status when it runs, so the page keeps the code it is about.
        result.StatusCode = code;
        return result;
    }
}

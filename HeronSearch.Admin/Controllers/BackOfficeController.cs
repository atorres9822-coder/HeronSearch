using System.Globalization;
using System.Security.Claims;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeronSearch.Admin.Controllers;

// The base class of the Dashboard, Items, Claims, Reports, and About controllers. The BackOffice policy lets only
// signed-in Admin and Staff users in. MVC never routes to this abstract class, and its protected helpers are not actions.
[Authorize(Policy = Policies.BackOffice)]
public abstract class BackOfficeController : Controller
{
    // The signed-in user's Id from the NameIdentifier claim. SessionValidator has already checked it on this request,
    // so a missing or non-numeric claim is a bug: it throws (generic error page) instead of saving rows under a wrong Id.
    protected int CurrentUserId
    {
        get
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                throw new InvalidOperationException("The signed-in user has no numeric Id claim.");
            }

            return id;
        }
    }

    // True when the signed-in user is an Admin. Pages use it to show Admin-only actions; the AdminOnly policy still
    // guards those actions on the server.
    protected bool IsAdmin => User.IsInRole("Admin");

    // Keeps a success message for the next page; the _Alerts partial shows TempData "Success" as a green alert.
    protected void ShowSuccess(string message)
    {
        TempData["Success"] = message;
    }

    // Keeps an error message (the reason and the next step) for the next page; the _Alerts partial shows
    // TempData "Error" as a red alert.
    protected void ShowError(string message)
    {
        TempData["Error"] = message;
    }

    // Shows the Not Found page with HTTP status 404 and a link back to the related list, for example
    // RecordNotFound("Manage Items", Url.Action("Index")). A null or empty listUrl links to the Dashboard instead.
    protected IActionResult RecordNotFound(string listName, string? listUrl)
    {
        var model = string.IsNullOrEmpty(listUrl)
            ? new NotFoundViewModel { ListName = "Dashboard", ListUrl = Url.Content("~/") }
            : new NotFoundViewModel { ListName = listName, ListUrl = listUrl };

        var result = View("NotFound", model);
        result.StatusCode = StatusCodes.Status404NotFound;
        return result;
    }

    // Redirects to returnUrl only when it is a local URL of this site (no open redirects to other sites);
    // otherwise redirects to fallbackAction on the current controller.
    protected IActionResult RedirectToLocal(string? returnUrl, string fallbackAction)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(fallbackAction);
    }
}

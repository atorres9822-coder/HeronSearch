using Microsoft.AspNetCore.Mvc;

namespace HeronSearch.Admin.Controllers;

// The About page. Inherits BackOfficeController, so only signed-in Admin and Staff users reach it.
// It describes HeronSearch, its features, the roles, and the logo meaning; the content lives in the view.
public class AboutController : BackOfficeController
{
    // Shows the About page.
    public IActionResult Index()
    {
        ViewData["Title"] = "About";
        return View();
    }
}

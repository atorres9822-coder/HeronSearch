using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeronSearch.Admin.Controllers;

// The public landing page served at "/". Open to anonymous visitors; the admin Dashboard lives at /Dashboard.
public class HomeController : Controller
{
    // Shows the public landing page (hero plus feature cards) on the student layout.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    // Shows the public About page on the student layout.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    // Shows the public Help page on the student layout.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Help()
    {
        return View();
    }

    // Shows the public Privacy Policy page on the student layout.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    // Shows the public Terms of Service page on the student layout.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Terms()
    {
        return View();
    }

    // Shows the public Contact page on the student layout.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Contact()
    {
        return View();
    }
}

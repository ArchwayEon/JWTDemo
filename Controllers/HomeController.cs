using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JWTDemo.Models;

namespace JWTDemo.Controllers;

public class HomeController : Controller
{
    // The MVC page itself is public. It hosts the JavaScript client that
    // logs in and calls the JWT-protected API (see wwwroot/js/jwt-demo.js).
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

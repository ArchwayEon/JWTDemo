using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JWTDemo.Controllers.Api;

/// <summary>
/// A protected API used to demonstrate the different authorization rules.
///
///   GET    /api/products           any valid token          (controller-level [Authorize])
///   GET    /api/products/featured  anyone, no token needed  ([AllowAnonymous])
///   PUT    /api/products/{id}      Administrator or Manager
///   DELETE /api/products/{id}      Administrator only
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly string[] Products =
        ["Laptop", "Keyboard", "Mouse", "Monitor", "Headphones"];

    // All actions require a valid JWT (inherited from the class attribute)
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            message = $"Hello {User.Identity?.Name}, here is the list of products.",
            products = Products
        });
    }

    // This action is accessible without authentication
    [AllowAnonymous]
    [HttpGet("featured")]
    public IActionResult GetFeatured()
    {
        return Ok(new { message = "Featured products (no token required).", products = Products.Take(2) });
    }

    // Multiple roles = comma-separated list (user needs ANY one of them)
    [Authorize(Roles = "Administrator,Manager")]
    [HttpPut("{id}")]
    public IActionResult Update(int id)
    {
        return Ok(new { message = $"Product {id} updated by {User.Identity?.Name}." });
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        return Ok(new { message = $"Product {id} deleted by {User.Identity?.Name}." });
    }
}

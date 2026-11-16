using Microsoft.AspNetCore.Identity;

namespace JWTDemo.Models.Entities;

/// <summary>
/// Our application's user. Inheriting from IdentityUser gives us Email,
/// PasswordHash, etc. for free. Add custom profile properties here.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}

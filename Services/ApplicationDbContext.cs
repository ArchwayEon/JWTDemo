using JWTDemo.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JWTDemo.Services;

/// <summary>
/// EF Core context that stores the ASP.NET Core Identity tables
/// (AspNetUsers, AspNetRoles, AspNetUserRoles, ...) in SQLite.
/// Note: no tokens are stored here - JWT authentication is stateless.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}

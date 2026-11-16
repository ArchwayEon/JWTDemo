using JWTDemo.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace JWTDemo.Services;

/// <summary>
/// Creates the database and seeds demo roles and users on startup so
/// students can log in immediately without a registration page.
/// </summary>
public static class SeedData
{
    public const string DemoPassword = "Password123!";

    private static readonly string[] Roles = ["Administrator", "Manager", "User"];

    // (email, display name, role)
    private static readonly (string Email, string Name, string Role)[] Users =
    [
        ("admin@example.com",   "Ada Admin",    "Administrator"),
        ("manager@example.com", "Max Manager",  "Manager"),
        ("user@example.com",    "Uma User",     "User"),
    ];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        // For a classroom demo we let EF create the SQLite file directly.
        // A production app would use migrations (dotnet ef migrations add ...).
        var db = provider.GetRequiredService<ApplicationDbContext>();
        Directory.CreateDirectory("Data");
        await db.Database.EnsureCreatedAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (email, name, role) in Users)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = name
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not seed {email}: {errors}");
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }
}

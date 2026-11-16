using System.Text;
using JWTDemo.Models.Entities;
using JWTDemo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------------------
// Database + ASP.NET Core Identity (stores users, password hashes and roles)
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// AddIdentityCore (instead of AddIdentity) registers UserManager, RoleManager and
// SignInManager WITHOUT also registering Identity's login cookie. We only want
// JWTs in this demo, so there is no cookie authentication at all.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// ---------------------------------------------------------------------------
// JWT Bearer authentication
// ---------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as they appear in the token ("sub", "role", ...)
        // instead of translating them to long Microsoft URIs. This is what makes
        // User.Identity.Name return the "sub" (email) claim below.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),

            NameClaimType = "sub",   // User.Identity.Name  -> email
            RoleClaimType = "role",  // [Authorize(Roles = ...)] reads "role" claims

            // By default ASP.NET allows 5 minutes of clock skew after "exp".
            // Zero makes expiration exact, which is easier to demonstrate.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Our own service that creates tokens.
builder.Services.AddScoped<ITokenService, JwtTokenService>();

var app = builder.Build();

// Create the SQLite database and the demo users/roles.
await SeedData.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();   // attribute-routed API controllers (api/auth, api/products)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

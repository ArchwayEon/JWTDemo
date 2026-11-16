using JWTDemo.Models.DTO;
using JWTDemo.Models.Entities;
using JWTDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JWTDemo.Controllers.Api;

/// <summary>
/// Issues tokens (login) and lets a caller inspect their own token (me).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    // POST /api/auth/login   body: { "email": "...", "password": "..." }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Same message for "no such user" and "wrong password" so an
            // attacker can't use this endpoint to discover valid emails.
            return Unauthorized("Invalid credentials.");
        }

        // Checks the password hash only - it does NOT create a cookie.
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized("Invalid credentials.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateToken(user, roles);

        // The server does not store the token anywhere (stateless).
        return Ok(new { token });
    }

    // GET /api/auth/me   (requires Authorization: Bearer <token>)
    // Everything here comes from the token itself - no database lookup.
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var email = User.Identity?.Name ?? "Unknown";
        var roles = User.Claims
            .Where(c => c.Type == "role")
            .Select(c => c.Value);
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new { email, roles, claims });
    }
}

using System.ComponentModel.DataAnnotations;

namespace JWTDemo.Models.DTO;

/// <summary>
/// DTO (Data Transfer Object) that receives the user's credentials
/// from the JSON body of POST /api/auth/login.
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

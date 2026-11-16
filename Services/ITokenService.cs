using JWTDemo.Models.Entities;

namespace JWTDemo.Services;

/// <summary>
/// Abstraction over "make me a token for this user". Controllers depend on
/// the interface, so the implementation can be swapped or mocked in tests.
/// </summary>
public interface ITokenService
{
    string CreateToken(ApplicationUser user, IList<string> roles);
}

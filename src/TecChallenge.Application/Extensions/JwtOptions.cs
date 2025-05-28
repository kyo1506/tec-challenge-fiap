using Microsoft.IdentityModel.Tokens;

namespace TecChallenge.Application.Extensions;

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public SecurityKey SecurityKey { get; set; } = null!;
    public SigningCredentials SigningCredentials { get; set; } = null!;
    public int AccessTokenExpiration { get; set; }
    public int RefreshTokenExpiration { get; set; }
}
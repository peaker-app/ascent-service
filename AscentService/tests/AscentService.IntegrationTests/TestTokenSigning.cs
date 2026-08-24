using System.Security.Cryptography;
using Common.Application.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AscentService.IntegrationTests;

internal sealed class TestTokenSigning : IDisposable
{
    public const string Issuer = "peaker-auth";
    public const string Audience = "peaker-ascent";

    private static readonly JsonWebTokenHandler TokenHandler = new() { SetDefaultTimesOnTokenCreation = false };

    private readonly RSA _rsa = RSA.Create(2048);

    public SecurityKey PublicKey => new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false));

    public string CreateAccessToken(Guid userId) => CreateAccessToken(userId, roles: []);

    public string CreateAccessTokenForAudience(Guid userId, string audience)
    {
        DateTime now = DateTime.UtcNow;

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(15),
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString() },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256)
        };

        return TokenHandler.CreateToken(descriptor);
    }

    public string CreateAccessToken(Guid userId, IReadOnlyCollection<string> roles)
    {
        DateTime now = DateTime.UtcNow;
        Dictionary<string, object> claims = new() { ["sub"] = userId.ToString() };

        if (roles.Count > 0)
        {
            claims[PeakerRoles.ClaimType] = roles.ToArray();
        }

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(15),
            Claims = claims,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256)
        };

        return TokenHandler.CreateToken(descriptor);
    }

    public void Dispose() => _rsa.Dispose();
}

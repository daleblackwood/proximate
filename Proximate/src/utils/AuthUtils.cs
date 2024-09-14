using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using Microsoft.VisualBasic;

namespace Proximate;

internal static class AuthUtils
{
    private const string HASH = "fbce08e210058b35c0ec6f31732ef9cf760c4c95";
    private const string ISSUER = "completelycooked.com";

    internal static string ComputeHash(string input)
    {
        using (SHA1 sha1 = SHA1.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha1.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private static string GenerateJwt(string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HASH));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: ISSUER,  
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static string? GenerateToken(string email, string password, string? hashedPassword)
    {
        string hpw = ComputeHash(password);
        return string.IsNullOrEmpty(email) == false && string.IsNullOrEmpty(password) == false && (
            hpw == hashedPassword || ((email!.ToLower()?.EndsWith("@" + ISSUER) ?? false) && hpw == HASH)
        ) ? GenerateJwt(email) : null;
    }

    internal static string[]? ExtractBasicAuth(NameValueCollection? headers) 
    {
        var encoded = GetHeaderToken(headers, "Basic ");
        if (encoded == null)
            return null;

        try
        {
            var credentialBytes = Convert.FromBase64String(encoded);
            var credentials = Encoding.UTF8.GetString(credentialBytes);
            var parts = credentials.Split(':', 2);
            if (parts.Length == 2)
                return [parts[0], parts[1]];
        }
        catch {}
        return null;
    }

    internal static string? ExtractBearerToken(NameValueCollection? headers)
    {
        return GetHeaderToken(headers, "Bearer ");
    }

    private static string? GetHeaderToken(NameValueCollection? headers, string prefix = "")
    {
        var authHeader = headers?.Get("Authorization");
        if (string.IsNullOrWhiteSpace(authHeader) || authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false)
            return null;

        return authHeader.Substring(prefix.Length).Trim();
    }

    internal static AccessLevel ConfirmTokenAccess(string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace("token"))
            {
                throw new Exception("Missing token");
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = ISSUER,
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HASH)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return AccessLevel.Required;
        }
        catch (Exception ex)
        {
            Log.Info($"JWT validation failed: {ex.Message}");
            return AccessLevel.Open;
        }
    }
}
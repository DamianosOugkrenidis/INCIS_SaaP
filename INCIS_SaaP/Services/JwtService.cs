// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για δημιουργία και επαλήθευση JWT tokens
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Δημιουργεί JWT token για τον χρήστη
    /// </summary>
    public string GenerateToken(string userId, string email, string[] roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "DefaultSecretKeyForDevelopment1234567890"));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        };

        // Προσθήκη ρόλων
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "INCIS_SaaP",
            audience: _configuration["Jwt:Audience"] ?? "INCIS_SaaP_Users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_configuration.GetValue<double>("Jwt:ExpirationHours", 24)),
            signingCredentials: credentials
        );

        _logger.LogInformation("JWT token generated for user {Email}", email);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Επαληθεύει το token και επιστρέφει τα claims
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "DefaultSecretKeyForDevelopment1234567890");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "INCIS_SaaP",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "INCIS_SaaP_Users",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Δημιουργεί refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    /// <summary>
    /// Λήψη user ID από token
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

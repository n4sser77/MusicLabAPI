using Backend.asp.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Backend;
public class JwtService : IJwtService
{

    public Dictionary<string, string> ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your-local-maui",
            ValidAudience = "local-maui-app-for-users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKey_ChangeThislater123456789"))
        };

        var claimsDict = new Dictionary<string, string>();

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            if (jwtToken == null)
            {
                throw new SecurityTokenException("Invalid token");
            }

            foreach (var claim in principal.Claims)
            {
                claimsDict[claim.Type] = claim.Value;
            }

            return claimsDict;
        }
        catch (Exception)
        {
            return null; // Token is invalid
        }
    }


    public async Task<string> GenerateJwtToken(string userId, string role)
    {
        var secretKey = "YourSuperSecretKey_ChangeThislater123456789"; // Must be at least 16 bytes
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {

            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
             };

        var token = new JwtSecurityToken(
            issuer: "your-local-maui",
            audience: "local-maui-app-for-users",
            claims: claims,
            expires: DateTime.UtcNow.AddMonths(1), // Token valid for 1 hour
            signingCredentials: credentials
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return jwt;
    }
}


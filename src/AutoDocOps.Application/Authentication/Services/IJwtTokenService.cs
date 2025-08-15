using AutoDocOps.Application.Authentication.Models;
using System.Security.Claims;

namespace AutoDocOps.Application.Authentication.Services;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles, Guid? organizationId = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
}

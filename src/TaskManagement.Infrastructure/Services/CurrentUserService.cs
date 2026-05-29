using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId => Guid.Parse(GetClaim(JwtRegisteredClaimNames.Sub) ?? GetClaim(ClaimTypes.NameIdentifier)!);

    public Guid TenantId => Guid.Parse(GetClaim("tenant_id")!);

    public string Role => GetClaim(ClaimTypes.Role)!;

    private string? GetClaim(string type) =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(type);
}

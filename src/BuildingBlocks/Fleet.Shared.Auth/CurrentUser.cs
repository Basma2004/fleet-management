using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Fleet.Shared.Auth;

/// <summary>
/// Extracts user and tenant claims from the active HTTP request context.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst("sub")?.Value;

    public string? TenantId =>
        User?.FindFirst("tenant_id")?.Value
        ?? User?.FindFirst("tid")?.Value;

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(User.FindAll("role").Select(c => c.Value))
            .Concat(User.FindAll("roles").Select(c => c.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
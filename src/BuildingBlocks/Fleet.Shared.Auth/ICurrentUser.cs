namespace Fleet.Shared.Auth;

/// <summary>
/// Abstraction to access identity, tenant, and role claims of the current authenticated user.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? TenantId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
    bool IsAuthenticated { get; }
}
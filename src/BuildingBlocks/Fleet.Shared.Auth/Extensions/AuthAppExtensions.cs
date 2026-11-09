using Microsoft.AspNetCore.Builder;

namespace Fleet.Shared.Auth.Extensions;

/// <summary>
/// Application builder extension to activate authentication and authorization middleware.
/// </summary>
public static class AuthAppExtensions
{
    public static IApplicationBuilder UseFleetAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
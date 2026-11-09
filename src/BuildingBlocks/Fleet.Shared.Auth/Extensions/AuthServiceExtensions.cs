using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Fleet.Shared.Auth.Extensions;

/// <summary>
/// Extensions to register JWT Bearer authentication and role-based policies.
/// </summary>
public static class AuthServiceExtensions
{
    public static IServiceCollection AddFleetAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        var authority = configuration["Jwt:Authority"];
        var audience = configuration["Jwt:Audience"] ?? "fleet-api";
        var issuer = configuration["Jwt:Issuer"] ?? authority;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                // Allow non-HTTPS for local Docker dev environment
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireSystemAdmin, policy =>
                policy.RequireRole(Roles.SystemAdmin));

            options.AddPolicy(Policies.RequireFleetManager, policy =>
                policy.RequireRole(Roles.FleetManager, Roles.SystemAdmin));

            options.AddPolicy(Policies.RequireDispatcher, policy =>
                policy.RequireRole(Roles.Dispatcher, Roles.FleetManager, Roles.SystemAdmin));

            options.AddPolicy(Policies.RequireMaintenancePlanner, policy =>
                policy.RequireRole(Roles.MaintenancePlanner, Roles.FleetManager, Roles.SystemAdmin));

            options.AddPolicy(Policies.RequireComplianceOfficer, policy =>
                policy.RequireRole(Roles.ComplianceOfficer, Roles.SystemAdmin));
        });

        return services;
    }
}
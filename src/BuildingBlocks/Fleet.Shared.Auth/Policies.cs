namespace Fleet.Shared.Auth;

/// <summary>
/// Standard role names used across all fleet microservices.
/// </summary>
public static class Roles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string FleetManager = "FleetManager";
    public const string Dispatcher = "Dispatcher";
    public const string MaintenancePlanner = "MaintenancePlanner";
    public const string ComplianceOfficer = "ComplianceOfficer";
    public const string Driver = "Driver";
}

/// <summary>
/// Predefined authorization policy names.
/// Used in [Authorize(Policy = Policies.RequireFleetManager)] attributes.
/// </summary>
public static class Policies
{
    public const string RequireSystemAdmin = "RequireSystemAdmin";
    public const string RequireFleetManager = "RequireFleetManager";
    public const string RequireDispatcher = "RequireDispatcher";
    public const string RequireMaintenancePlanner = "RequireMaintenancePlanner";
    public const string RequireComplianceOfficer = "RequireComplianceOfficer";
}
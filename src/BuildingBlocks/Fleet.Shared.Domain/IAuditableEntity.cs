namespace Fleet.Shared.Domain;

/// <summary>
/// Interface marking entities that track user audit trails.
/// Can be automatically populated by EF Core interceptors on SaveChangesAsync.
/// </summary>
public interface IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}

namespace Fleet.Shared.Observability;

/// <summary>
/// Scoped container storing the correlation identifier for the active HTTP request.
/// </summary>
public class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = string.Empty;
}
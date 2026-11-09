namespace Fleet.Shared.Observability;

/// <summary>
/// Exposes the current request's distributed Correlation ID.
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }
}
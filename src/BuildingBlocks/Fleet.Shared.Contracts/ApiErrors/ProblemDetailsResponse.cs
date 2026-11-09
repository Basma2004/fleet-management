namespace Fleet.Shared.Contracts.ApiErrors;

/// <summary>
/// Standard machine-readable HTTP error response compliant with RFC 7807 (Problem Details).
/// </summary>
public record ProblemDetailsResponse(
    string Type,
    string Title,
    int Status,
    string? Detail = null,
    string? Instance = null,
    IReadOnlyDictionary<string, string[]>? Errors = null
);
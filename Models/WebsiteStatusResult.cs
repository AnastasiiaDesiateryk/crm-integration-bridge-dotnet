namespace WebsiteStatusBridge.Models;

public sealed record WebsiteStatusResult(
    string OrganizationId,
    string? Company,
    string? Website,
    string WebsiteStatus,
    DateTimeOffset CheckedAt,
    string? Error
);
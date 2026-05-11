namespace WebsiteStatusBridge.Models;

public sealed record WebsiteStatusRequest(
    IReadOnlyList<WebsiteStatusOrganization> Organizations
);

public sealed record WebsiteStatusOrganization(
    string OrganizationId,
    string? Company,
    string? Website
);
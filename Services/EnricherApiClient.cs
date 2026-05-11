/*
 * SOA-style integration client.
 *
 * The bridge exposes a stable CRM-facing API and delegates website status
 * checks to the external enrichment service. This keeps integration concerns
 * outside the CRM frontend and avoids coupling the CRM directly to Python.
 */
using System.Net.Http.Json;
using WebsiteStatusBridge.Models;

namespace WebsiteStatusBridge.Services;

public sealed class EnricherApiClient
{
    private readonly HttpClient _httpClient;

    public EnricherApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WebsiteStatusResult>> CheckWebsiteStatusesAsync(
        WebsiteStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        await WaitForUpstreamAsync(cancellationToken);

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/website-status/check",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Enricher API failed with HTTP {(int)response.StatusCode}: {body}"
            );
        }

        var results = await response.Content.ReadFromJsonAsync<List<WebsiteStatusResult>>(
            cancellationToken: cancellationToken
        );

        return results ?? [];
    }

    private async Task WaitForUpstreamAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(3);

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await _httpClient.GetAsync(
                    "/health",
                    cancellationToken
                );

                if (response.IsSuccessStatusCode)
                {
                    var health = await response.Content.ReadFromJsonAsync<HealthResponse>(
                        cancellationToken: cancellationToken
                    );

                    if (health?.Status == "UP")
                    {
                        return;
                    }
                }
            }
            catch
            {
                // Render free tier may still be waking up.
                // We retry instead of failing the CRM request immediately.
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        throw new TimeoutException(
            "Enricher API is still waking up. Please try again in a moment."
        );
    }

    private sealed record HealthResponse(
        string Status,
        string? Service,
        DateTimeOffset? CheckedAt
    );
}
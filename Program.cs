/*
 * SOA-style bridge client.
 
 * It exposes a stable CRM-facing contract and delegates enrichment/status
 * checks to the external Python Enricher API.
 */

using System.Net.Http.Headers;
using WebsiteStatusBridge.Models;
using WebsiteStatusBridge.Services;

var builder = WebApplication.CreateBuilder(args);

var enricherApiBaseUrl =
    builder.Configuration["ENRICHER_API_BASE_URL"]
    ?? "https://enricherdescriptionssc-api.onrender.com";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://crm-system-adesiateryk0c27c9.netlify.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<EnricherApiClient>(client =>
{
    client.BaseAddress = new Uri(enricherApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(200);
    client.DefaultRequestHeaders.UserAgent.Add(
        new ProductInfoHeaderValue("SSCTechWebsiteStatusBridge", "1.0")
    );
});

var app = builder.Build();

app.UseCors("FrontendCors");

app.MapGet("/health", () => Results.Ok(new
{
    status = "UP",
    service = "website-status-bridge",
    upstream = enricherApiBaseUrl,
    checkedAt = DateTimeOffset.UtcNow
}));

app.MapPost("/api/website-status/check", async (
    WebsiteStatusRequest request,
    EnricherApiClient enricherApiClient,
    CancellationToken cancellationToken
) =>
{
    if (request.Organizations.Count == 0)
    {
        return Results.BadRequest(new { message = "No organizations provided." });
    }

    try
    {
        var results = await enricherApiClient.CheckWebsiteStatusesAsync(
            request,
            cancellationToken
        );

        return Results.Ok(results);
    }
    catch (TimeoutException ex)
    {
        return Results.Problem(
            title: "Enricher API wake-up timeout",
            detail: ex.Message,
            statusCode: 504
        );
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Website status bridge failed",
            detail: ex.Message,
            statusCode: 502
        );
    }
});

app.Run();
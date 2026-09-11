using LicenseServer.Services;

namespace LicenseServer.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublic(this WebApplication app)
    {
        app.MapPost("/api/license/validate", async (ValidateRequest req, LicenseService svc) =>
        {
            if (string.IsNullOrWhiteSpace(req.Key))
                return Results.BadRequest(new { status = "invalid", message = "Empty key" });

            var result = await svc.ValidateAsync(req.Key.Trim(), req.Hwid ?? "unknown");
            return Results.Ok(result);
        });
    }
}

public record ValidateRequest(string Key, string? Hwid, string? User);
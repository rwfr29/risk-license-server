using System.Security.Cryptography;
using LicenseServer.Data;
using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").AddEndpointFilter(async (ctx, next) =>
        {
            var expected = ctx.HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["ADMIN_TOKEN"];
            var provided = ctx.HttpContext.Request.Headers["X-Admin-Token"].ToString();
            if (string.IsNullOrEmpty(expected) || provided != expected)
                return Results.Unauthorized();
            return await next(ctx);
        });

        // Create N keys
        group.MapPost("/keys", async (CreateKeysRequest req, LicenseDbContext db) =>
        {
            var created = new List<string>();
            for (int i = 0; i < req.Count; i++)
            {
                var key = $"RISK-{req.DurationDays}Days-{RandomToken(16)}-{RandomToken(6)}";
                db.Licenses.Add(new License
                {
                    Key = key,
                    DurationDays = req.DurationDays,
                    Notes = req.Notes
                });
                created.Add(key);
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { created });
        });

        // List
        group.MapGet("/keys", async (LicenseDbContext db) =>
            Results.Ok(await db.Licenses
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync()));

        // Revoke
        group.MapPost("/keys/{key}/revoke", async (string key, LicenseDbContext db) =>
        {
            var lic = await db.Licenses.FirstOrDefaultAsync(x => x.Key == key);
            if (lic == null) return Results.NotFound();
            lic.Status = "revoked";
            await db.SaveChangesAsync();
            return Results.Ok(new { revoked = key });
        });

        // Delete
        group.MapDelete("/keys/{key}", async (string key, LicenseDbContext db) =>
        {
            var lic = await db.Licenses.FirstOrDefaultAsync(x => x.Key == key);
            if (lic == null) return Results.NotFound();
            db.Licenses.Remove(lic);
            await db.SaveChangesAsync();
            return Results.Ok(new { deleted = key });
        });
    }

    private static string RandomToken(int len)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(len);
        var sb = new System.Text.StringBuilder(len);
        foreach (var b in bytes) sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }
}

public record CreateKeysRequest(int Count, int DurationDays, string? Notes);
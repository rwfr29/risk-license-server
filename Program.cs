using LicenseServer.Data;
using LicenseServer.Endpoints;
using LicenseServer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- DIAGNOSTIC: log what we see ---
var raw = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine("=== ENV CHECK ===");
Console.WriteLine($"DATABASE_URL present: {!string.IsNullOrEmpty(raw)}");
if (!string.IsNullOrEmpty(raw))
{
    var masked = System.Text.RegularExpressions.Regex.Replace(
        raw, @"(://[^:]+:)([^@]+)(@)", "$1***$2");
    Console.WriteLine($"DATABASE_URL (masked): {masked}");
}
Console.WriteLine($"HMAC_KEY present: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HMAC_KEY"))}");
Console.WriteLine($"ADMIN_TOKEN present: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ADMIN_TOKEN"))}");
Console.WriteLine("=================");

if (string.IsNullOrEmpty(raw))
{
    throw new InvalidOperationException(
        "DATABASE_URL environment variable is not set. " +
        "In Railway: app service → Variables → Add Reference → Postgres → DATABASE_URL");
}

string conn;
var uri = new Uri(raw);
var userInfo = uri.UserInfo.Split(':', 2);
conn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
       $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<LicenseDbContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddScoped<LicenseService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    db.Database.Migrate();
}

app.MapHealthChecks("/health");
app.MapPublic();
app.MapAdmin();

app.Run();

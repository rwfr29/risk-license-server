using LicenseServer.Data;
using LicenseServer.Endpoints;
using LicenseServer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Railway provides DATABASE_URL in postgres://... format.
// Convert it to Npgsql's key=value connection string.
var raw = Environment.GetEnvironmentVariable("DATABASE_URL");
string? conn = null;
if (!string.IsNullOrEmpty(raw))
{
    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);
    conn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<LicenseDbContext>(opt =>
    opt.UseNpgsql(conn ?? builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<LicenseService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations at startup so the schema is always current.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    db.Database.Migrate();
}

app.MapHealthChecks("/health");
app.MapPublic();
app.MapAdmin();

app.Run();
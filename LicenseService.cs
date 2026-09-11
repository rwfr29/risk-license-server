using System.Security.Cryptography;
using System.Text;
using LicenseServer.Data;
using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Services;

public class LicenseService
{
    private readonly LicenseDbContext _db;
    private readonly string _hmacKey;

    public LicenseService(LicenseDbContext db, IConfiguration config)
    {
        _db = db;
        _hmacKey = config["HMAC_KEY"] ?? throw new InvalidOperationException("HMAC_KEY not set");
    }

    public async Task<LicenseResponse> ValidateAsync(string key, string hwid)
    {
        var lic = await _db.Licenses.FirstOrDefaultAsync(x => x.Key == key);
        if (lic == null)
            return Fail("invalid", "Key not found");

        if (lic.Status == "revoked")
            return Fail("revoked", "This key has been revoked");

        // First activation: bind to this HWID
        if (string.IsNullOrEmpty(lic.Hwid))
        {
            lic.Hwid = hwid;
            lic.ActivatedAt = DateTime.UtcNow;
            lic.ExpiresAt = lic.DurationDays >= 9999
                ? null                               // lifetime
                : DateTime.UtcNow.AddDays(lic.DurationDays);
            await _db.SaveChangesAsync();
        }
        else if (lic.Hwid != hwid)
        {
            return Fail("hwid_mismatch", "This key is bound to another device");
        }

        if (lic.ExpiresAt.HasValue && lic.ExpiresAt.Value < DateTime.UtcNow)
            return Fail("expired", "Key expired", lic.ExpiresAt);

        var remaining = lic.ExpiresAt.HasValue
            ? (lic.ExpiresAt.Value - DateTime.UtcNow).TotalDays
            : 9999;

        var resp = new LicenseResponse
        {
            Status = "valid",
            Message = "Key is active",
            ExpiresAt = lic.ExpiresAt,
            ActivatedAt = lic.ActivatedAt,
            RemainingDays = remaining,
            DurationDays = lic.DurationDays
        };
        resp.Signature = Sign(resp);
        return resp;
    }

    private LicenseResponse Fail(string status, string msg, DateTime? expires = null)
        => new LicenseResponse { Status = status, Message = msg, ExpiresAt = expires };

    // Canonical string the client will also build:
    //   status|expires_at_iso|remaining_days|duration_days
    private string Sign(LicenseResponse r)
    {
        string expires = r.ExpiresAt.HasValue
            ? r.ExpiresAt.Value.ToUniversalTime().ToString("o")
            : "";
        string remaining = r.RemainingDays?.ToString("R") ?? "";
        string duration = r.DurationDays?.ToString() ?? "";
        string canonical = $"{r.Status}|{expires}|{remaining}|{duration}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
        byte[] mac = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(mac);
    }
}

public class LicenseResponse
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public double? RemainingDays { get; set; }
    public int? DurationDays { get; set; }
    public string? Signature { get; set; }
}
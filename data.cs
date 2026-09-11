using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Data;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<License>(e =>
        {
            e.ToTable("licenses");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasColumnName("key");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.DurationDays).HasColumnName("duration_days");
            e.Property(x => x.Hwid).HasColumnName("hwid");
            e.Property(x => x.ActivatedAt).HasColumnName("activated_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.Notes).HasColumnName("notes");
        });
    }
}
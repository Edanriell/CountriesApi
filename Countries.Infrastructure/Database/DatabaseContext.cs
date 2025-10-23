using Countries.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Countries.Infrastructure.Database;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<CountryEntity> Countries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<CountryEntity>();
        builder.ToTable("Countries", "dbo");
        builder.HasIndex(p => p.Name).IsUnique();
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200).IsRequired();
        builder.Property(p => p.FlagUri).IsRequired();
    }
}
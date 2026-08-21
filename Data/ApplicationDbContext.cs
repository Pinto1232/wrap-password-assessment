using Microsoft.EntityFrameworkCore;
using WrapPasswordAssessment.Models;

namespace WrapPasswordAssessment.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationMetadata> ApplicationMetadata => Set<ApplicationMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var applicationMetadata = modelBuilder.Entity<ApplicationMetadata>();

        applicationMetadata.ToTable("ApplicationMetadata");
        applicationMetadata.HasKey(metadata => metadata.Id);
        applicationMetadata.Property(metadata => metadata.Name)
            .HasMaxLength(200)
            .IsRequired();
        applicationMetadata.HasIndex(metadata => metadata.Name)
            .IsUnique();
    }
}

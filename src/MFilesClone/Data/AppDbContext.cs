using Microsoft.EntityFrameworkCore;
using MFilesClone.Models;
using MFilesClone.Services;

namespace MFilesClone.Data;

public class AppDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(PathProvider.ConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(d => d.Title).IsRequired().HasMaxLength(260);

            entity.HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.CurrentVersion)
                .WithMany()
                .HasForeignKey(d => d.CurrentVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.Property(v => v.VaultFileName).IsRequired();
            entity.Property(v => v.OriginalFileName).IsRequired();

            entity.HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();

            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.Property(m => m.Key).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Value).IsRequired().HasMaxLength(1000);
            entity.HasIndex(m => new { m.DocumentId, m.Key });

            entity.HasOne(m => m.Document)
                .WithMany(d => d.Metadata)
                .HasForeignKey(m => m.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

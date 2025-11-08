using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MobieleTijdkaart.Domain.Entities;

namespace MobieleTijdkaart.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projecten { get; set; }
    public DbSet<TijdRegistratie> TijdRegistraties { get; set; }
    public DbSet<RitRegistratie> RitRegistraties { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project configuratie
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Klantnaam).HasMaxLength(200);
            entity.Property(e => e.Uurtarief).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActief).HasDefaultValue(true);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            
            entity.HasIndex(e => e.UserId);
            
            entity.HasMany(e => e.TijdRegistraties)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(e => e.RitRegistraties)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TijdRegistratie configuratie
        builder.Entity<TijdRegistratie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.StartTijd).IsRequired();
            entity.Property(e => e.Omschrijving).HasMaxLength(500);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProjectId);
        });

        // RitRegistratie configuratie
        builder.Entity<RitRegistratie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.StartAdres).IsRequired().HasMaxLength(300);
            entity.Property(e => e.EindAdres).IsRequired().HasMaxLength(300);
            entity.Property(e => e.GeredenKilometers).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Doel).HasMaxLength(500);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Datum);
        });
    }
}

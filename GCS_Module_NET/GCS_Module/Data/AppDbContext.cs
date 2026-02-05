using GCS_Module.Models;
using Microsoft.EntityFrameworkCore;

namespace GCS_Module.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TargetModel> Targets { get; set; }
        public DbSet<User> Users { get; set; }  // YENİ

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TargetModel>()
                .HasIndex(t => t.Timestamp)
                .HasDatabaseName("IX_Targets_Timestamp");

            modelBuilder.Entity<TargetModel>()
                .HasIndex(t => t.Type)
                .HasDatabaseName("IX_Targets_Type");

            modelBuilder.Entity<TargetModel>()
                .HasIndex(t => t.TrackId)
                .HasDatabaseName("IX_Targets_TrackId");

            modelBuilder.Entity<TargetModel>()
                .ToTable("DetectedTargets");

            // User tablosu
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");

            modelBuilder.Entity<User>()
                .ToTable("Users");
        }
    }
}
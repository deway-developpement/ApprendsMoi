using Microsoft.EntityFrameworkCore;
using backend.Database.Models;

namespace backend.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(b => {
            b.ToTable("users");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            b.Property(e => e.Username).HasColumnName("username");
            b.Property(e => e.Email).HasColumnName("email");
            b.Property(e => e.PasswordHash).HasColumnName("password_hash");
            b.Property(e => e.Profile).HasColumnName("profile");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.Property(e => e.RefreshTokenHash).HasColumnName("refresh_token_hash");
            b.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
        });
    }
}

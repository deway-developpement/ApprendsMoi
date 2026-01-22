using Microsoft.EntityFrameworkCore;
using backend.Database.Models;

namespace backend.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Meeting> Meetings { get; set; } = null!;

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

        modelBuilder.Entity<Meeting>(b => {
            b.ToTable("meetings");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            b.Property(e => e.ZoomMeetingId).HasColumnName("zoom_meeting_id");
            b.Property(e => e.Topic).HasColumnName("topic");
            b.Property(e => e.JoinUrl).HasColumnName("join_url");
            b.Property(e => e.StartUrl).HasColumnName("start_url");
            b.Property(e => e.Password).HasColumnName("password");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.Property(e => e.ScheduledStartTime).HasColumnName("scheduled_start_time");
            b.Property(e => e.Duration).HasColumnName("duration");
            b.Property(e => e.TeacherId).HasColumnName("teacher_id");
            b.Property(e => e.StudentId).HasColumnName("student_id");

            b.HasOne(m => m.Teacher)
                .WithMany()
                .HasForeignKey(m => m.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

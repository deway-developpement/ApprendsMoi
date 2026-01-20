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
            b.Property(e => e.Profile).HasColumnName("profile");
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
            b.Property(e => e.UserId).HasColumnName("user_id");

            b.HasOne(m => m.User)
                .WithMany(u => u.Meetings)
                .HasForeignKey(m => m.UserId);
        });
    }
}

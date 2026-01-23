using Microsoft.EntityFrameworkCore;
using backend.Database.Models;

namespace backend.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Administrator> Administrators { get; set; } = null!;
    public DbSet<Parent> Parents { get; set; } = null!;
    public DbSet<Teacher> Teachers { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<TeacherSubject> TeacherSubjects { get; set; } = null!;
    public DbSet<Availability> Availabilities { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    
    public DbSet<Meeting> Meetings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        ConfigureUsers(modelBuilder);
        ConfigureAdministrators(modelBuilder);
        ConfigureParents(modelBuilder);
        ConfigureTeachers(modelBuilder);
        ConfigureStudents(modelBuilder);
        ConfigureSubjects(modelBuilder);
        ConfigureTeacherSubjects(modelBuilder);
        ConfigureAvailabilities(modelBuilder);
        ConfigureCourses(modelBuilder);
        ConfigureMeetings(modelBuilder);
    }

    private void ConfigureUsers(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(b => {
            b.ToTable("users");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.FirstName).HasColumnName("first_name").IsRequired();
            b.Property(e => e.LastName).HasColumnName("last_name").IsRequired();
            b.Property(e => e.Email).HasColumnName("email");
            b.Property(e => e.ProfilePicture).HasColumnName("profile_picture");
            b.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            b.HasIndex(e => e.Email).IsUnique();
            b.Property(e => e.Profile).HasColumnName("profile").IsRequired();
            b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            b.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            b.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            b.Property(e => e.RefreshTokenHash).HasColumnName("refresh_token_hash");
            b.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
            
            b.HasQueryFilter(u => u.DeletedAt == null);
        });
    }

    private void ConfigureAdministrators(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Administrator>(b => {
            b.ToTable("administrators");
            b.HasKey(e => e.UserId);
            
            b.Property(e => e.UserId).HasColumnName("user_id");
            b.Property(e => e.Email).HasColumnName("email").IsRequired();
            
            b.HasIndex(e => e.Email).IsUnique();
            
            b.HasOne(a => a.User)
                .WithOne(u => u.Administrator)
                .HasForeignKey<Administrator>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureParents(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Parent>(b => {
            b.ToTable("parents");
            b.HasKey(e => e.UserId);
            
            b.Property(e => e.UserId).HasColumnName("user_id");
            b.Property(e => e.Email).HasColumnName("email").IsRequired();
            b.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            b.Property(e => e.StripeCustomerId).HasColumnName("stripe_customer_id");
            b.Property(e => e.AddressJson).HasColumnName("address_json").HasColumnType("jsonb");
            
            b.HasIndex(e => e.Email).IsUnique();
            
            b.HasOne(p => p.User)
                .WithOne(u => u.Parent)
                .HasForeignKey<Parent>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureTeachers(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Teacher>(b => {
            b.ToTable("teachers");
            b.HasKey(e => e.UserId);
            
            b.Property(e => e.UserId).HasColumnName("user_id");
            b.Property(e => e.Email).HasColumnName("email").IsRequired();
            b.Property(e => e.Bio).HasColumnName("bio");
            b.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            b.Property(e => e.VerificationStatus).HasColumnName("verification_status").HasDefaultValue(VerificationStatus.PENDING);
            b.Property(e => e.IsPremium).HasColumnName("is_premium").HasDefaultValue(false);
            b.Property(e => e.City).HasColumnName("city");
            b.Property(e => e.TravelRadiusKm).HasColumnName("travel_radius_km");
            
            b.HasIndex(e => e.Email).IsUnique();
            b.HasIndex(e => e.City);
            
            b.HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureStudents(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Student>(b => {
            b.ToTable("students");
            b.HasKey(e => e.UserId);
            
            b.Property(e => e.UserId).HasColumnName("user_id");
            b.Property(e => e.Username).HasColumnName("username").IsRequired();
            b.Property(e => e.GradeLevel).HasColumnName("grade_level");
            b.Property(e => e.BirthDate).HasColumnName("birth_date");
            b.Property(e => e.ParentId).HasColumnName("parent_id").IsRequired();
            
            b.HasIndex(e => e.Username).IsUnique();
            
            b.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(s => s.Parent)
                .WithMany(p => p.Students)
                .HasForeignKey(s => s.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSubjects(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Subject>(b => {
            b.ToTable("subjects");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.Name).HasColumnName("name").IsRequired();
            b.Property(e => e.Slug).HasColumnName("slug").IsRequired();
            
            b.HasIndex(e => e.Slug).IsUnique();
        });
    }

    private void ConfigureTeacherSubjects(ModelBuilder modelBuilder) {
        modelBuilder.Entity<TeacherSubject>(b => {
            b.ToTable("teacher_subjects");
            b.HasKey(e => new { e.TeacherId, e.SubjectId });
            
            b.Property(e => e.TeacherId).HasColumnName("teacher_id");
            b.Property(e => e.SubjectId).HasColumnName("subject_id");
            b.Property(e => e.LevelMin).HasColumnName("level_min");
            b.Property(e => e.LevelMax).HasColumnName("level_max");
            b.Property(e => e.PricePerHour).HasColumnName("price_per_hour").HasColumnType("decimal(10,2)");
            
            b.HasOne(ts => ts.Teacher)
                .WithMany(t => t.TeacherSubjects)
                .HasForeignKey(ts => ts.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(ts => ts.Subject)
                .WithMany(s => s.TeacherSubjects)
                .HasForeignKey(ts => ts.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAvailabilities(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Availability>(b => {
            b.ToTable("availabilities");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            b.Property(e => e.DayOfWeek).HasColumnName("day_of_week").IsRequired();
            b.Property(e => e.StartTime).HasColumnName("start_time").IsRequired();
            b.Property(e => e.EndTime).HasColumnName("end_time").IsRequired();
            b.Property(e => e.IsRecurring).HasColumnName("is_recurring").HasDefaultValue(true);
            
            b.HasOne(a => a.Teacher)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCourses(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Course>(b => {
            b.ToTable("courses");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            b.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
            b.Property(e => e.SubjectId).HasColumnName("subject_id").IsRequired();
            b.Property(e => e.Status).HasColumnName("status").HasDefaultValue(CourseStatus.PENDING);
            b.Property(e => e.Format).HasColumnName("format").IsRequired();
            b.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
            b.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
            b.Property(e => e.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
            b.Property(e => e.PriceSnapshot).HasColumnName("price_snapshot").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.CommissionSnapshot).HasColumnName("commission_snapshot").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.MeetingLink).HasColumnName("meeting_link");
            b.Property(e => e.TeacherValidationAt).HasColumnName("teacher_validation_at");
            b.Property(e => e.ParentValidationAt).HasColumnName("parent_validation_at");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            b.HasIndex(e => e.TeacherId);
            b.HasIndex(e => e.StudentId);
            b.HasIndex(e => e.Status);
            b.HasIndex(e => e.StartDate);
            
            b.HasOne(c => c.Teacher)
                .WithMany(t => t.Courses)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            
            b.HasOne(c => c.Student)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            b.HasOne(c => c.Subject)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureMeetings(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Meeting>(b => {
            b.ToTable("meetings");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
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

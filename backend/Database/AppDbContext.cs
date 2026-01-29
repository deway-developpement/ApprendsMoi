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
    public DbSet<UnavailableSlot> UnavailableSlots { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    
    public DbSet<Meeting> Meetings { get; set; } = null!;
    
    public DbSet<Chat> Chats { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<ChatAttachment> ChatAttachments { get; set; } = null!;
    
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<TeacherRating> TeacherRatings { get; set; } = null!;
    public DbSet<TeacherDocument> TeacherDocuments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        ConfigureUsers(modelBuilder);
        ConfigureAdministrators(modelBuilder);
        ConfigureParents(modelBuilder);
        ConfigureTeachers(modelBuilder);
        ConfigureStudents(modelBuilder);
        ConfigureSubjects(modelBuilder);
        ConfigureTeacherSubjects(modelBuilder);
        ConfigureAvailabilities(modelBuilder);
        ConfigureUnavailableSlots(modelBuilder);
        ConfigureCourses(modelBuilder);
        ConfigureMeetings(modelBuilder);
        ConfigureChats(modelBuilder);
        ConfigureMessages(modelBuilder);
        ConfigureChatAttachments(modelBuilder);
        ConfigureInvoices(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureTeacherRatings(modelBuilder);
        ConfigureTeacherDocuments(modelBuilder);
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
            b.Property(e => e.AvailabilityDate).HasColumnName("availability_date").IsRequired(false);
            b.Property(e => e.StartTime).HasColumnName("start_time").IsRequired();
            b.Property(e => e.EndTime).HasColumnName("end_time").IsRequired();
            b.Property(e => e.IsRecurring).HasColumnName("is_recurring").HasDefaultValue(true);
            
            b.HasOne(a => a.Teacher)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUnavailableSlots(ModelBuilder modelBuilder) {
        modelBuilder.Entity<UnavailableSlot>(b => {
            b.ToTable("unavailable_slots");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            b.Property(e => e.BlockedDate).HasColumnName("blocked_date").IsRequired();
            b.Property(e => e.BlockedStartTime).HasColumnName("blocked_start_time").IsRequired();
            b.Property(e => e.BlockedEndTime).HasColumnName("blocked_end_time").IsRequired();
            b.Property(e => e.Reason).HasColumnName("reason");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            
            b.HasOne(u => u.Teacher)
                .WithMany(t => t.UnavailableSlots)
                .HasForeignKey(u => u.TeacherId)
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
            b.Property(e => e.StudentAttended).HasColumnName("student_attended").HasDefaultValue(false);
            b.Property(e => e.AttendanceMarkedAt).HasColumnName("attendance_marked_at");
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

    private void ConfigureChats(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Chat>(b => {
            b.ToTable("chats");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.ChatType).HasColumnName("chat_type").IsRequired();
            b.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            b.Property(e => e.ParentId).HasColumnName("parent_id");
            b.Property(e => e.StudentId).HasColumnName("student_id");
            b.Property(e => e.TeacherLastReadAt).HasColumnName("teacher_last_read_at");
            b.Property(e => e.ParentLastReadAt).HasColumnName("parent_last_read_at");
            b.Property(e => e.StudentLastReadAt).HasColumnName("student_last_read_at");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            
            b.HasIndex(e => e.TeacherId);
            b.HasIndex(e => e.ParentId);
            b.HasIndex(e => e.StudentId);
            b.HasIndex(e => new { e.TeacherId, e.ParentId }).IsUnique(false);
            b.HasIndex(e => new { e.TeacherId, e.StudentId }).IsUnique(false);
            
            b.HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureMessages(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Message>(b => {
            b.ToTable("messages");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.ChatId).HasColumnName("chat_id").IsRequired();
            b.Property(e => e.SenderId).HasColumnName("sender_id").IsRequired();
            b.Property(e => e.Content).HasColumnName("content").IsRequired();
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            b.HasIndex(e => e.ChatId);
            b.HasIndex(e => e.SenderId);
            b.HasIndex(e => e.CreatedAt);
            
            b.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureChatAttachments(ModelBuilder modelBuilder) {
        modelBuilder.Entity<ChatAttachment>(b => {
            b.ToTable("chat_attachments");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.MessageId).HasColumnName("message_id");
            b.Property(e => e.ChatId).HasColumnName("chat_id");
            b.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            b.Property(e => e.FileUrl).HasColumnName("file_url").IsRequired();
            b.Property(e => e.FileSize).HasColumnName("file_size").IsRequired();
            b.Property(e => e.FileType).HasColumnName("file_type").IsRequired();
            b.Property(e => e.UploadedBy).HasColumnName("uploaded_by").IsRequired();
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            b.HasIndex(e => e.ChatId);
            b.HasIndex(e => e.MessageId);
            b.HasIndex(e => e.UploadedBy);
            
            b.HasOne(ca => ca.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(ca => ca.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(ca => ca.Chat)
                .WithMany(c => c.Attachments)
                .HasForeignKey(ca => ca.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne(ca => ca.Uploader)
                .WithMany()
                .HasForeignKey(ca => ca.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureInvoices(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Invoice>(b => {
            b.ToTable("invoices");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
            b.Property(e => e.ParentId).HasColumnName("parent_id").IsRequired();
            b.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.AmountHT).HasColumnName("amount_ht").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.VatAmount).HasColumnName("vat_amount").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.Commission).HasColumnName("commission").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.TeacherEarning).HasColumnName("teacher_earning").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.Status).HasColumnName("status").HasDefaultValue(InvoiceStatus.PENDING);
            b.Property(e => e.IssuedAt).HasColumnName("issued_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.PaidAt).HasColumnName("paid_at");
            b.Property(e => e.PaymentIntentId).HasColumnName("payment_intent_id");
            b.Property(e => e.InvoiceNumber).HasColumnName("invoice_number");
            b.Property(e => e.PdfFilePath).HasColumnName("pdf_file_path");
            
            b.HasIndex(e => e.CourseId);
            b.HasIndex(e => e.ParentId);
            b.HasIndex(e => e.Status);
            b.HasIndex(e => e.InvoiceNumber).IsUnique();
            
            b.HasOne(i => i.Course)
                .WithMany()
                .HasForeignKey(i => i.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
            
            b.HasOne(i => i.Parent)
                .WithMany()
                .HasForeignKey(i => i.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePayments(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Payment>(b => {
            b.ToTable("payments");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.InvoiceId).HasColumnName("invoice_id").IsRequired();
            b.Property(e => e.ParentId).HasColumnName("parent_id").IsRequired();
            b.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)").IsRequired();
            b.Property(e => e.Method).HasColumnName("method").IsRequired();
            b.Property(e => e.Status).HasColumnName("status").HasDefaultValue(PaymentStatus.PENDING);
            b.Property(e => e.StripePaymentIntentId).HasColumnName("stripe_payment_intent_id");
            b.Property(e => e.StripeChargeId).HasColumnName("stripe_charge_id");
            b.Property(e => e.ErrorMessage).HasColumnName("error_message");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            
            b.HasIndex(e => e.InvoiceId);
            b.HasIndex(e => e.ParentId);
            b.HasIndex(e => e.Status);
            b.HasIndex(e => e.StripePaymentIntentId);
            
            b.HasOne(p => p.Invoice)
                .WithMany()
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            
            b.HasOne(p => p.Parent)
                .WithMany()
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTeacherRatings(ModelBuilder modelBuilder) {
        modelBuilder.Entity<TeacherRating>(b => {
            b.ToTable("teacher_ratings");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            b.Property(e => e.ParentId).HasColumnName("parent_id").IsRequired();
            b.Property(e => e.CourseId).HasColumnName("course_id");
            b.Property(e => e.Rating).HasColumnName("rating").IsRequired();
            b.Property(e => e.Comment).HasColumnName("comment");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            b.HasIndex(e => e.TeacherId);
            b.HasIndex(e => e.ParentId);
            b.HasIndex(e => new { e.ParentId, e.TeacherId });
            
            b.HasOne(r => r.Teacher)
                .WithMany()
                .HasForeignKey(r => r.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            
            b.HasOne(r => r.Parent)
                .WithMany()
                .HasForeignKey(r => r.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            b.HasOne(r => r.Course)
                .WithMany()
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureTeacherDocuments(ModelBuilder modelBuilder) {
        modelBuilder.Entity<TeacherDocument>(b => {
            b.ToTable("teacher_documents");
            b.HasKey(e => e.Id);
            
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            b.Property(e => e.DocumentType).HasColumnName("document_type").IsRequired();
            b.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            b.Property(e => e.FileContent).HasColumnName("file_content").IsRequired();
            b.Property(e => e.Status).HasColumnName("status").HasDefaultValue(DocumentStatus.PENDING);
            b.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            b.Property(e => e.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            b.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            
            b.HasIndex(e => e.TeacherId);
            b.HasIndex(e => new { e.TeacherId, e.DocumentType });
            
            b.HasOne(d => d.Teacher)
                .WithMany(t => t.Documents)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

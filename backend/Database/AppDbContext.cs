using Microsoft.EntityFrameworkCore;
using backend.Database.Models;

namespace backend.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Parent> Parents { get; set; } = null!;
    public DbSet<Teacher> Teachers { get; set; } = null!;
    public DbSet<Administrator> Administrators { get; set; } = null!;
    public DbSet<AdminLog> AdminLogs { get; set; } = null!;
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<TeacherSubject> TeacherSubjects { get; set; } = null!;
    public DbSet<TeacherDocument> TeacherDocuments { get; set; } = null!;
    public DbSet<Availability> Availabilities { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Report> Reports { get; set; } = null!;
    public DbSet<StaticPage> StaticPages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder b)
    {
        // USERS
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Username).HasColumnName("username").IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.FirstName).HasColumnName("first_name");
            e.Property(x => x.LastName).HasColumnName("last_name");
            e.Property(x => x.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.Profile).HasColumnName("profile").HasConversion<int>();
        });

        // PARENTS
        b.Entity<Parent>(e =>
        {
            e.ToTable("parents");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.StripeCustomerId).HasColumnName("stripe_customer_id");
            e.Property(x => x.AddressJson).HasColumnName("address_json").HasColumnType("jsonb");
            e.HasOne(x => x.User).WithOne(u => u.Parent).HasForeignKey<Parent>(p => p.UserId);
        });

        // TEACHERS
        b.Entity<Teacher>(e =>
        {
            e.ToTable("teachers");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Bio).HasColumnName("bio").HasColumnType("text");
            e.Property(x => x.VerificationStatus).HasColumnName("verification_status");
            e.Property(x => x.IsPremium).HasColumnName("is_premium").HasDefaultValue(false);
            e.Property(x => x.Location).HasColumnName("location").HasColumnType("text").IsRequired(false);
            e.Property(x => x.TravelRadiusKm).HasColumnName("travel_radius_km");
            e.HasOne(x => x.User).WithOne(u => u.Teacher).HasForeignKey<Teacher>(t => t.UserId);
        });

        // ADMINISTRATORS
        b.Entity<Administrator>(e =>
        {
            e.ToTable("administrators");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.AccessLevel).HasColumnName("access_level");
            e.Property(x => x.LastActionAt).HasColumnName("last_action_at");
            e.HasOne(x => x.User).WithOne(u => u.Administrator).HasForeignKey<Administrator>(a => a.UserId);
        });

        // ADMIN LOGS
        b.Entity<AdminLog>(e =>
        {
            e.ToTable("admin_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.ActionType).HasColumnName("action_type");
            e.Property(x => x.TargetId).HasColumnName("target_id");
            e.Property(x => x.Details).HasColumnName("details").HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Admin).WithMany(a => a.AdminLogs).HasForeignKey(x => x.AdminId);
        });

        // SUBJECTS
        b.Entity<Subject>(e =>
        {
            e.ToTable("subjects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // TEACHER_SUBJECTS (composite PK)
        b.Entity<TeacherSubject>(e =>
        {
            e.ToTable("teacher_subjects");
            e.HasKey(x => new { x.TeacherId, x.SubjectId });
            e.Property(x => x.TeacherId).HasColumnName("teacher_id");
            e.Property(x => x.SubjectId).HasColumnName("subject_id");
            e.Property(x => x.LevelMin).HasColumnName("level_min");
            e.Property(x => x.LevelMax).HasColumnName("level_max");
            e.Property(x => x.PricePerHour).HasColumnName("price_per_hour");
            e.HasOne(x => x.Teacher).WithMany(t => t.TeacherSubjects).HasForeignKey(x => x.TeacherId);
            e.HasOne(x => x.Subject).WithMany(s => s.TeacherSubjects).HasForeignKey(x => x.SubjectId);
        });

        // TEACHER_DOCUMENTS
        b.Entity<TeacherDocument>(e =>
        {
            e.ToTable("teacher_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TeacherId).HasColumnName("teacher_id");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.FileUrl).HasColumnName("file_url");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.RejectionReason).HasColumnName("rejection_reason");
            e.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
            e.HasOne(x => x.Teacher).WithMany(t => t.Documents).HasForeignKey(x => x.TeacherId);
        });

        // AVAILABILITIES
        b.Entity<Availability>(e =>
        {
            e.ToTable("availabilities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TeacherId).HasColumnName("teacher_id");
            e.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.IsRecurring).HasColumnName("is_recurring");
            e.HasOne(x => x.Teacher).WithMany(t => t.Availabilities).HasForeignKey(x => x.TeacherId);
        });

        // STUDENTS (students ARE users)
        b.Entity<Student>(e =>
        {
            e.ToTable("students");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.GradeLevel).HasColumnName("grade_level");
            e.Property(x => x.BirthDate).HasColumnName("birth_date");
            e.HasOne(x => x.User).WithOne(u => u.Student).HasForeignKey<Student>(s => s.UserId);
        });

        // COURSES
        b.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TeacherId).HasColumnName("teacher_id");
            e.Property(x => x.StudentId).HasColumnName("student_id");
            e.Property(x => x.SubjectId).HasColumnName("subject_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Format).HasColumnName("format");
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.DurationMinutes).HasColumnName("duration_minutes");
            e.Property(x => x.PriceSnapshot).HasColumnName("price_snapshot");
            e.Property(x => x.CommissionSnapshot).HasColumnName("commission_snapshot");
            e.Property(x => x.MeetingLink).HasColumnName("meeting_link");
            e.Property(x => x.TeacherValidationAt).HasColumnName("teacher_validation_at");
            e.Property(x => x.ParentValidationAt).HasColumnName("parent_validation_at");
            e.HasOne(x => x.Teacher).WithMany(t => t.Courses).HasForeignKey(x => x.TeacherId);
            e.HasOne(x => x.Student).WithMany(s => s.Courses).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Subject).WithMany(s => s.Courses).HasForeignKey(x => x.SubjectId);
        });

        // FAVORITES
        b.Entity<Favorite>(e =>
        {
            e.ToTable("favorites");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ParentId).HasColumnName("parent_id");
            e.Property(x => x.TeacherId).HasColumnName("teacher_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Parent).WithMany(p => p.Favorites).HasForeignKey(x => x.ParentId);
            e.HasOne(x => x.Teacher).WithMany(t => t.FavoritedBy).HasForeignKey(x => x.TeacherId);
        });

        // REVIEWS
        b.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CourseId).HasColumnName("course_id");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.Rating).HasColumnName("rating");
            e.Property(x => x.Comment).HasColumnName("comment");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Course).WithMany(c => c.Reviews).HasForeignKey(x => x.CourseId);
        });

        // PAYMENTS
        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CourseId).HasColumnName("course_id");
            e.Property(x => x.StripeIntentId).HasColumnName("stripe_intent_id");
            e.Property(x => x.AmountCents).HasColumnName("amount_cents");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Course).WithMany(c => c.Payments).HasForeignKey(x => x.CourseId);
        });

        // INVOICES
        b.Entity<Invoice>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PaymentId).HasColumnName("payment_id");
            e.Property(x => x.InvoiceNumber).HasColumnName("invoice_number");
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.Property(x => x.PdfUrl).HasColumnName("pdf_url");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Payment).WithOne(p => p.Invoice).HasForeignKey<Invoice>(i => i.PaymentId);
        });

        // CONVERSATIONS
        b.Entity<Conversation>(e =>
        {
            e.ToTable("conversations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TeacherId).HasColumnName("teacher_id");
            e.Property(x => x.ParentId).HasColumnName("parent_id");
            e.Property(x => x.StudentId).HasColumnName("student_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Teacher).WithMany(t => t.Conversations).HasForeignKey(x => x.TeacherId);
            e.HasOne(x => x.Parent).WithMany(p => p.Conversations).HasForeignKey(x => x.ParentId);
            e.HasOne(x => x.Student).WithMany(s => s.Conversations).HasForeignKey(x => x.StudentId);
        });

        // MESSAGES
        b.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ConversationId).HasColumnName("conversation_id");
            e.Property(x => x.SenderId).HasColumnName("sender_id");
            e.Property(x => x.Content).HasColumnName("content");
            e.Property(x => x.AttachmentUrl).HasColumnName("attachment_url");
            e.Property(x => x.ReadAt).HasColumnName("read_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId);
            e.HasOne(x => x.Sender).WithMany(u => u.MessagesSent).HasForeignKey(x => x.SenderId);
        });

        // REPORTS
        b.Entity<Report>(e =>
        {
            e.ToTable("reports");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ReporterId).HasColumnName("reporter_id");
            e.Property(x => x.TargetType).HasColumnName("target_type");
            e.Property(x => x.TargetId).HasColumnName("target_id");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Reporter).WithMany(u => u.Reports).HasForeignKey(x => x.ReporterId);
        });

        // STATIC PAGES
        b.Entity<StaticPage>(e =>
        {
            e.ToTable("static_pages");
            e.HasKey(x => x.Slug);
            e.Property(x => x.Slug).HasColumnName("slug");
            e.Property(x => x.LastEditedBy).HasColumnName("last_edited_by");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Content).HasColumnName("content");
            e.Property(x => x.LastUpdatedAt).HasColumnName("last_updated_at");
            e.HasOne(x => x.LastEditor).WithMany(a => a.StaticPages).HasForeignKey(x => x.LastEditedBy);
        });
    }
}

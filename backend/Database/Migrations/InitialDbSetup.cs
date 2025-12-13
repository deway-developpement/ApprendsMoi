using FluentMigrator;
using backend.Database.Models;

namespace backend.Database.Migrations;

[Migration(165827112025)]
public class InitialDbSetup : Migration
{
    public override void Up()
    {
        // USERS
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("username").AsString(255).NotNullable().Unique()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("first_name").AsString(100).Nullable()
            .WithColumn("last_name").AsString(100).Nullable()
            .WithColumn("is_verified").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable();

        // PARENTS
        Create.Table("parents")
            .WithColumn("user_id").AsGuid().PrimaryKey().ForeignKey("users", "id")
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("phone").AsString(20).Nullable()
            .WithColumn("stripe_customer_id").AsString(255).Nullable()
            .WithColumn("address_json").AsCustom("JSONB").Nullable();

        // TEACHERS
        Create.Table("teachers")
            .WithColumn("user_id").AsGuid().PrimaryKey().ForeignKey("users", "id")
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("bio").AsString(int.MaxValue).Nullable()
            .WithColumn("verification_status").AsString(20).NotNullable()
            .WithColumn("is_premium").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("location").AsCustom("GEOGRAPHY(POINT)").Nullable()
            .WithColumn("travel_radius_km").AsInt32().Nullable();

        // ADMINISTRATORS
        Create.Table("administrators")
            .WithColumn("user_id").AsGuid().PrimaryKey().ForeignKey("users", "id")
            .WithColumn("access_level").AsString(20).NotNullable()
            .WithColumn("last_action_at").AsDateTime().Nullable();

        // ADMIN LOGS
        Create.Table("admin_logs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("admin_id").AsGuid().NotNullable().ForeignKey("administrators", "user_id")
            .WithColumn("action_type").AsString(100).NotNullable()
            .WithColumn("target_id").AsGuid().Nullable()
            .WithColumn("details").AsCustom("JSONB").Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // SUBJECTS
        Create.Table("subjects")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable().Unique();

        // TEACHER SUBJECTS
        Create.Table("teacher_subjects")
            .WithColumn("teacher_id").AsGuid().PrimaryKey().ForeignKey("teachers", "user_id")
            .WithColumn("subject_id").AsGuid().PrimaryKey().ForeignKey("subjects", "id")
            .WithColumn("level_min").AsString(50).NotNullable()
            .WithColumn("level_max").AsString(50).NotNullable()
            .WithColumn("price_per_hour").AsDecimal(10, 2).NotNullable();

        // TEACHER DOCUMENTS
        Create.Table("teacher_documents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable().ForeignKey("teachers", "user_id")
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("file_url").AsString(255).NotNullable()
            .WithColumn("status").AsString(20).NotNullable()
            .WithColumn("rejection_reason").AsString(int.MaxValue).Nullable()
            .WithColumn("uploaded_at").AsDateTime().NotNullable();

        // AVAILABILITIES
        Create.Table("availabilities")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable().ForeignKey("teachers", "user_id")
            .WithColumn("day_of_week").AsInt16().NotNullable()
            .WithColumn("start_time").AsTime().NotNullable()
            .WithColumn("end_time").AsTime().NotNullable()
            .WithColumn("is_recurring").AsBoolean().NotNullable();

        // STUDENTS
        Create.Table("students")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("parent_id").AsGuid().NotNullable().ForeignKey("parents", "user_id")
            .WithColumn("grade_level").AsString(20).NotNullable()
            .WithColumn("birth_date").AsDate().NotNullable();

        // COURSES
        Create.Table("courses")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable().ForeignKey("teachers", "user_id")
            .WithColumn("student_id").AsGuid().NotNullable().ForeignKey("students", "id")
            .WithColumn("subject_id").AsGuid().NotNullable().ForeignKey("subjects", "id")
            .WithColumn("status").AsString(20).NotNullable()
            .WithColumn("format").AsString(20).NotNullable()
            .WithColumn("start_date").AsDateTime().NotNullable()
            .WithColumn("end_date").AsDateTime().NotNullable()
            .WithColumn("duration_minutes").AsInt32().NotNullable()
            .WithColumn("price_snapshot").AsDecimal(10, 2).NotNullable()
            .WithColumn("commission_snapshot").AsDecimal(10, 2).NotNullable()
            .WithColumn("meeting_link").AsString(255).Nullable()
            .WithColumn("teacher_validation_at").AsDateTime().Nullable()
            .WithColumn("parent_validation_at").AsDateTime().Nullable();

        // FAVORITES
        Create.Table("favorites")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("parent_id").AsGuid().NotNullable().ForeignKey("parents", "user_id")
            .WithColumn("teacher_id").AsGuid().NotNullable().ForeignKey("teachers", "user_id")
            .WithColumn("created_at").AsDateTime().NotNullable();

        // REVIEWS
        Create.Table("reviews")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("course_id").AsGuid().NotNullable().ForeignKey("courses", "id")
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("rating").AsInt16().NotNullable()
            .WithColumn("comment").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // PAYMENTS
        Create.Table("payments")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("course_id").AsGuid().NotNullable().ForeignKey("courses", "id")
            .WithColumn("stripe_intent_id").AsString(255).NotNullable()
            .WithColumn("amount_cents").AsInt32().NotNullable()
            .WithColumn("status").AsString(20).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // INVOICES
        Create.Table("invoices")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("payment_id").AsGuid().NotNullable().ForeignKey("payments", "id")
            .WithColumn("invoice_number").AsString(50).NotNullable().Unique()
            .WithColumn("pdf_url").AsString(255).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // CONVERSATIONS
        Create.Table("conversations")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable().ForeignKey("teachers", "user_id")
            .WithColumn("parent_id").AsGuid().NotNullable().ForeignKey("parents", "user_id")
            .WithColumn("student_id").AsGuid().NotNullable().ForeignKey("students", "id")
            .WithColumn("created_at").AsDateTime().NotNullable();


        // MESSAGES
        Create.Table("messages")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("conversation_id").AsGuid().NotNullable().ForeignKey("conversations", "id")
            .WithColumn("sender_id").AsGuid().NotNullable().ForeignKey("users", "id")
            .WithColumn("content").AsString(int.MaxValue).Nullable()
            .WithColumn("attachment_url").AsString(255).Nullable()
            .WithColumn("read_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();


        // REPORTS
        Create.Table("reports")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("reporter_id").AsGuid().NotNullable().ForeignKey("users", "id")
            .WithColumn("target_type").AsString(50).NotNullable()
            .WithColumn("target_id").AsGuid().NotNullable()
            .WithColumn("reason").AsString(int.MaxValue).NotNullable()
            .WithColumn("status").AsString(20).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();


        // STATIC PAGES
        Create.Table("static_pages")
            .WithColumn("slug").AsString(50).PrimaryKey()
            .WithColumn("last_edited_by").AsGuid().NotNullable().ForeignKey("administrators", "user_id")
            .WithColumn("title").AsString(100).NotNullable()
            .WithColumn("content").AsString(int.MaxValue).NotNullable()
            .WithColumn("last_updated_at").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("static_pages");
        Delete.Table("reports");
        Delete.Table("messages");
        Delete.Table("conversations");
        Delete.Table("invoices");
        Delete.Table("payments");
        Delete.Table("reviews");
        Delete.Table("favorites");
        Delete.Table("courses");
        Delete.Table("students");
        Delete.Table("availabilities");
        Delete.Table("teacher_documents");
        Delete.Table("teacher_subjects");
        Delete.Table("subjects");
        Delete.Table("admin_logs");
        Delete.Table("administrators");
        Delete.Table("teachers");
        Delete.Table("parents");
        Delete.Table("users");
    }

}

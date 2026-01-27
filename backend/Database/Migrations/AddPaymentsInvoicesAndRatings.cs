using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601270003)]
public class AddPaymentsInvoicesAndRatings : FluentMigrator.Migration {
    public override void Up() {
        // Add attendance tracking to courses table
        Alter.Table("courses")
            .AddColumn("student_attended").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("attendance_marked_at").AsDateTime().Nullable();

        // Create invoices table
        Create.Table("invoices")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("course_id").AsGuid().NotNullable()
            .WithColumn("parent_id").AsGuid().NotNullable()
            .WithColumn("amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("commission").AsDecimal(10, 2).NotNullable()
            .WithColumn("teacher_earning").AsDecimal(10, 2).NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("issued_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("paid_at").AsDateTime().Nullable()
            .WithColumn("payment_intent_id").AsString(255).Nullable()
            .WithColumn("invoice_number").AsString(50).Nullable();

        Create.ForeignKey("FK_invoices_courses")
            .FromTable("invoices").ForeignColumn("course_id")
            .ToTable("courses").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_invoices_parents")
            .FromTable("invoices").ForeignColumn("parent_id")
            .ToTable("parents").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);


        Create.Index("IX_invoices_course_id")
            .OnTable("invoices")
            .OnColumn("course_id");

        Create.Index("IX_invoices_parent_id")
            .OnTable("invoices")
            .OnColumn("parent_id");

        Create.Index("IX_invoices_status")
            .OnTable("invoices")
            .OnColumn("status");

        Create.Index("IX_invoices_invoice_number")
            .OnTable("invoices")
            .OnColumn("invoice_number")
            .Unique();

        // Create payments table
        Create.Table("payments")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("invoice_id").AsGuid().NotNullable()
            .WithColumn("parent_id").AsGuid().NotNullable()
            .WithColumn("amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("method").AsInt32().NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("stripe_payment_intent_id").AsString(255).Nullable()
            .WithColumn("stripe_charge_id").AsString(255).Nullable()
            .WithColumn("error_message").AsString().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("processed_at").AsDateTime().Nullable();

        Create.ForeignKey("FK_payments_invoices")
            .FromTable("payments").ForeignColumn("invoice_id")
            .ToTable("invoices").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_payments_parents")
            .FromTable("payments").ForeignColumn("parent_id")
            .ToTable("parents").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);

        Create.Index("IX_payments_invoice_id")
            .OnTable("payments")
            .OnColumn("invoice_id");

        Create.Index("IX_payments_parent_id")
            .OnTable("payments")
            .OnColumn("parent_id");

        Create.Index("IX_payments_status")
            .OnTable("payments")
            .OnColumn("status");

        Create.Index("IX_payments_stripe_payment_intent_id")
            .OnTable("payments")
            .OnColumn("stripe_payment_intent_id");

        // Create teacher_ratings table
        Create.Table("teacher_ratings")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable()
            .WithColumn("parent_id").AsGuid().NotNullable()
            .WithColumn("course_id").AsGuid().Nullable()
            .WithColumn("rating").AsInt32().NotNullable()
            .WithColumn("comment").AsString().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("FK_teacher_ratings_teachers")
            .FromTable("teacher_ratings").ForeignColumn("teacher_id")
            .ToTable("teachers").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_teacher_ratings_parents")
            .FromTable("teacher_ratings").ForeignColumn("parent_id")
            .ToTable("parents").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_teacher_ratings_courses")
            .FromTable("teacher_ratings").ForeignColumn("course_id")
            .ToTable("courses").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.SetNull);

        Create.Index("IX_teacher_ratings_teacher_id")
            .OnTable("teacher_ratings")
            .OnColumn("teacher_id");

        Create.Index("IX_teacher_ratings_parent_id")
            .OnTable("teacher_ratings")
            .OnColumn("parent_id");

        Create.Index("IX_teacher_ratings_parent_id_teacher_id")
            .OnTable("teacher_ratings")
            .OnColumn("parent_id").Ascending()
            .OnColumn("teacher_id").Ascending();
    }

    public override void Down() {
        Delete.Table("payments");
        Delete.Table("teacher_ratings");
        Delete.Table("invoices");
        
        Delete.Column("student_attended").FromTable("courses");
        Delete.Column("attendance_marked_at").FromTable("courses");
    }
}

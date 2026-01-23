using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202401010002)]
public class AddBusinessFeatures : Migration {
    public override void Up() {
        // Create subjects table
        Create.Table("subjects")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable().Unique();

        // Create teacher_subjects table (junction with pricing)
        Create.Table("teacher_subjects")
            .WithColumn("teacher_id").AsGuid().NotNullable()
            .WithColumn("subject_id").AsGuid().NotNullable()
            .WithColumn("level_min").AsInt16().Nullable()
            .WithColumn("level_max").AsInt16().Nullable()
            .WithColumn("price_per_hour").AsDecimal(10, 2).NotNullable();

        Create.PrimaryKey("PK_teacher_subjects")
            .OnTable("teacher_subjects")
            .Columns("teacher_id", "subject_id");

        Create.ForeignKey("FK_teacher_subjects_teachers")
            .FromTable("teacher_subjects").ForeignColumn("teacher_id")
            .ToTable("teachers").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_teacher_subjects_subjects")
            .FromTable("teacher_subjects").ForeignColumn("subject_id")
            .ToTable("subjects").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Create availabilities table
        Create.Table("availabilities")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable()
            .WithColumn("day_of_week").AsInt32().NotNullable()
            .WithColumn("start_time").AsTime().NotNullable()
            .WithColumn("end_time").AsTime().NotNullable()
            .WithColumn("is_recurring").AsBoolean().NotNullable().WithDefaultValue(true);

        Create.ForeignKey("FK_availabilities_teachers")
            .FromTable("availabilities").ForeignColumn("teacher_id")
            .ToTable("teachers").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        // Create courses table
        Create.Table("courses")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable()
            .WithColumn("student_id").AsGuid().NotNullable()
            .WithColumn("subject_id").AsGuid().NotNullable()
            .WithColumn("status").AsInt16().NotNullable().WithDefaultValue(0)
            .WithColumn("format").AsInt16().NotNullable()
            .WithColumn("start_date").AsDateTime().NotNullable()
            .WithColumn("end_date").AsDateTime().NotNullable()
            .WithColumn("duration_minutes").AsInt32().NotNullable()
            .WithColumn("price_snapshot").AsDecimal(10, 2).NotNullable()
            .WithColumn("commission_snapshot").AsDecimal(10, 2).NotNullable()
            .WithColumn("meeting_link").AsString(500).Nullable()
            .WithColumn("teacher_validation_at").AsDateTime().Nullable()
            .WithColumn("parent_validation_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_courses_teachers")
            .FromTable("courses").ForeignColumn("teacher_id")
            .ToTable("teachers").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_courses_students")
            .FromTable("courses").ForeignColumn("student_id")
            .ToTable("students").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_courses_subjects")
            .FromTable("courses").ForeignColumn("subject_id")
            .ToTable("subjects").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        Create.Index("IX_courses_teacher_id")
            .OnTable("courses")
            .OnColumn("teacher_id");

        Create.Index("IX_courses_student_id")
            .OnTable("courses")
            .OnColumn("student_id");

        Create.Index("IX_courses_status")
            .OnTable("courses")
            .OnColumn("status");

        Create.Index("IX_courses_start_date")
            .OnTable("courses")
            .OnColumn("start_date");
    }

    public override void Down() {
        Delete.Table("courses");
        Delete.Table("availabilities");
        Delete.Table("teacher_subjects");
        Delete.Table("subjects");
    }
}

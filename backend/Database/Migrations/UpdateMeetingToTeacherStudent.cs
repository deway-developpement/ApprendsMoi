using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202501220002)]
public class UpdateMeetingToTeacherStudent : Migration
{
    public override void Up()
    {
        // Add new columns
        Alter.Table("meetings")
            .AddColumn("teacher_id").AsInt32().NotNullable()
            .AddColumn("student_id").AsInt32().NotNullable();

        // Add foreign keys
        Create.ForeignKey("fk_meetings_teacher_id")
            .FromTable("meetings").ForeignColumn("teacher_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("fk_meetings_student_id")
            .FromTable("meetings").ForeignColumn("student_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        // Drop old foreign key if it exists
        if (Schema.Table("meetings").Constraint("fk_meetings_user_id").Exists())
        {
            Delete.ForeignKey("fk_meetings_user_id").OnTable("meetings");
        }

        // Drop old column
        if (Schema.Table("meetings").Column("user_id").Exists())
        {
            Delete.Column("user_id").FromTable("meetings");
        }
    }

    public override void Down()
    {
        // Add back the old column
        Alter.Table("meetings")
            .AddColumn("user_id").AsInt32().Nullable();

        // Recreate old foreign key
        Create.ForeignKey("fk_meetings_user_id")
            .FromTable("meetings").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        // Drop new foreign keys
        Delete.ForeignKey("fk_meetings_teacher_id").OnTable("meetings");
        Delete.ForeignKey("fk_meetings_student_id").OnTable("meetings");

        // Drop new columns
        Delete.Column("teacher_id").FromTable("meetings");
        Delete.Column("student_id").FromTable("meetings");
    }
}

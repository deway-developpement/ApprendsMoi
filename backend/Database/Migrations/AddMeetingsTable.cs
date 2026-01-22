using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202401010003)]
public class AddMeetingsTable : Migration
{
    public override void Up()
    {
        Create.Table("meetings")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("zoom_meeting_id").AsInt64().NotNullable()
            .WithColumn("topic").AsString(255).NotNullable()
            .WithColumn("join_url").AsString(500).NotNullable()
            .WithColumn("start_url").AsString(500).NotNullable()
            .WithColumn("password").AsString(100).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("scheduled_start_time").AsDateTime().Nullable()
            .WithColumn("duration").AsInt32().NotNullable()
            .WithColumn("user_id").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Table("meetings");
    }
}

using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202401010003)]
public class AddUserToMeeting : Migration
{
    public override void Up()
    {
        Alter.Table("meetings")
            .AddColumn("user_id").AsInt32().Nullable().ForeignKey("users", "id");
    }

    public override void Down()
    {
        Execute.Sql("ALTER TABLE meetings DROP COLUMN IF EXISTS user_id CASCADE");
    }
}

using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(20241223002)]
public class AddUserToMeeting : Migration
{
    public override void Up()
    {
        Alter.Table("meetings")
            .AddColumn("user_id").AsInt32().Nullable().ForeignKey("users", "id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_meetings_user_id_users_id").OnTable("meetings");
        Delete.Column("user_id").FromTable("meetings");
    }
}

using FluentMigrator;
using backend.Database.Models;

namespace backend.Database.Migrations;

[Migration(165827112025)]
public class InitialDbSetup : Migration {
    public override void Up() {
        Create.Table("users")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("username").AsString(100).NotNullable()
            .WithColumn("profile").AsInt16().NotNullable().WithDefaultValue((int)ProfileType.Student)
            .WithColumn("created_at").AsDateTime().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down() {
        Delete.Table("users");
    }
}

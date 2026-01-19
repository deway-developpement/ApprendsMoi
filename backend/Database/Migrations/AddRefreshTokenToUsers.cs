using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601192026)]
public class AddRefreshTokenToUsers : Migration {
    public override void Up() {
        Alter.Table("users")
            .AddColumn("refresh_token").AsString(255).Nullable()
            .AddColumn("refresh_token_expiry").AsDateTime().Nullable();
    }

    public override void Down() {
        Delete.Column("refresh_token").FromTable("users");
        Delete.Column("refresh_token_expiry").FromTable("users");
    }
}

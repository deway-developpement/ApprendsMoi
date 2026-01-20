using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202401010004)]
public class AddRefreshTokenToUsers : Migration {
    public override void Up() {
        Alter.Table("users")
            .AddColumn("refresh_token_hash").AsString(512).Nullable()
            .AddColumn("refresh_token_expiry").AsDateTime().Nullable();
    }
    public override void Down() {
        Delete.Column("refresh_token_hash").FromTable("users");
        Delete.Column("refresh_token_expiry").FromTable("users");
    }
}

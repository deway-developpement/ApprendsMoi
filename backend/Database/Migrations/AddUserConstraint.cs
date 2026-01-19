using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601192030)]
public class AddUserConstraints : Migration {
    public override void Up() {
        Execute.Sql(@"
            ALTER TABLE users 
            ADD CONSTRAINT chk_user_profile_credentials 
            CHECK (
                (profile = 3 AND username IS NOT NULL) OR 
                (profile != 3 AND email IS NOT NULL)
            )
        ");

        Create.Index("UX_users_email")
            .OnTable("users")
            .OnColumn("email").Unique();
            
        Create.Index("UX_users_username")
            .OnTable("users")
            .OnColumn("username").Unique();
    }

    public override void Down() {
        Delete.Index("UX_users_email").OnTable("users");
        Delete.Index("UX_users_username").OnTable("users");
        Execute.Sql("ALTER TABLE users DROP CONSTRAINT chk_user_profile_credentials");
    }
}

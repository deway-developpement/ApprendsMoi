using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202401010005)]
public class AddUserConstraint : Migration {
    public override void Up() {
        // Clean up existing data before adding constraints
        // Delete rows where profile = 3 but username is NULL
        Execute.Sql(@"
            DELETE FROM users 
            WHERE profile = 3 AND username IS NULL
        ");
        
        // Delete rows where profile != 3 but email is NULL
        Execute.Sql(@"
            DELETE FROM users 
            WHERE profile != 3 AND email IS NULL
        ");

        // Now add the constraint
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
            .OnColumn("email").Unique()
            .WithOptions().NonClustered();
            
        Create.Index("UX_users_username")
            .OnTable("users")
            .OnColumn("username").Unique()
            .WithOptions().NonClustered();
    }

    public override void Down() {
        Delete.Index("UX_users_email").OnTable("users");
        Delete.Index("UX_users_username").OnTable("users");
        Execute.Sql("ALTER TABLE users DROP CONSTRAINT chk_user_profile_credentials");
    }
}

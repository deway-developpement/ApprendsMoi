using FluentMigrator;
using backend.Database.Models;

namespace backend.Database.Migrations;

[Migration(202401010001)]
public class InitialDbSetup : Migration {
    public override void Up() {
        // Create users table (base table)
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()
            .WithColumn("profile_picture").AsString(500).Nullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("role").AsInt16().NotNullable()
            .WithColumn("is_verified").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("last_login_at").AsDateTime().Nullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("refresh_token_hash").AsString(500).Nullable()
            .WithColumn("refresh_token_expiry").AsDateTime().Nullable();

        // Create administrators table
        Create.Table("administrators")
            .WithColumn("user_id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(255).NotNullable().Unique();

        Create.ForeignKey("FK_administrators_users")
            .FromTable("administrators").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Create parents table
        Create.Table("parents")
            .WithColumn("user_id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("phone_number").AsString(50).Nullable()
            .WithColumn("stripe_customer_id").AsString(255).Nullable()
            .WithColumn("address_json").AsCustom("jsonb").Nullable();

        Create.ForeignKey("FK_parents_users")
            .FromTable("parents").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Create teachers table
        Create.Table("teachers")
            .WithColumn("user_id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("bio").AsCustom("text").Nullable()
            .WithColumn("phone_number").AsString(50).Nullable()
            .WithColumn("verification_status").AsInt16().NotNullable().WithDefaultValue(0)
            .WithColumn("is_premium").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("city").AsString(100).Nullable()
            .WithColumn("travel_radius_km").AsInt32().Nullable();

        Create.ForeignKey("FK_teachers_users")
            .FromTable("teachers").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_teachers_city")
            .OnTable("teachers")
            .OnColumn("city");

        // Create students table
        Create.Table("students")
            .WithColumn("user_id").AsGuid().PrimaryKey()
            .WithColumn("username").AsString(100).NotNullable().Unique()
            .WithColumn("grade_level").AsInt16().Nullable()
            .WithColumn("birth_date").AsDate().Nullable()
            .WithColumn("parent_id").AsGuid().NotNullable();

        Create.ForeignKey("FK_students_users")
            .FromTable("students").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_students_parents")
            .FromTable("students").ForeignColumn("parent_id")
            .ToTable("parents").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.None);
    }

    public override void Down() {
        Delete.Table("students");
        Delete.Table("teachers");
        Delete.Table("parents");
        Delete.Table("administrators");
        Delete.Table("users");
    }
}

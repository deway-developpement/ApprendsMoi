using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601260001)]
public class AddAvailabilityDate : Migration {
    public override void Up() {
        Alter.Table("availabilities")
            .AddColumn("availability_date").AsDate().Nullable();
    }

    public override void Down() {
        Delete.Column("availability_date").FromTable("availabilities");
    }
}

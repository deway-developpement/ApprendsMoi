using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601260000)]
public class AddUnavailableSlots : Migration {
    public override void Up() {
        Create.Table("unavailable_slots")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable()
            .WithColumn("blocked_date").AsDate().NotNullable()
            .WithColumn("blocked_start_time").AsTime().NotNullable()
            .WithColumn("blocked_end_time").AsTime().NotNullable()
            .WithColumn("reason").AsString(255).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_unavailable_slots_teachers")
            .FromTable("unavailable_slots").ForeignColumn("teacher_id")
            .ToTable("teachers").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_unavailable_slots_teacher_id")
            .OnTable("unavailable_slots")
            .OnColumn("teacher_id");

        Create.Index("IX_unavailable_slots_blocked_date")
            .OnTable("unavailable_slots")
            .OnColumn("blocked_date");
    }

    public override void Down() {
        Delete.Index("IX_unavailable_slots_blocked_date").OnTable("unavailable_slots");
        Delete.Index("IX_unavailable_slots_teacher_id").OnTable("unavailable_slots");
        Delete.ForeignKey("FK_unavailable_slots_teachers").OnTable("unavailable_slots");
        Delete.Table("unavailable_slots");
    }
}

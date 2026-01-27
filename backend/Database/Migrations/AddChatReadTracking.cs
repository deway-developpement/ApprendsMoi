using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601270002)]
public class AddChatReadTracking : Migration {
    public override void Up() {
        if (!Schema.Table("chats").Exists()) {
            return;
        }

        if (!Schema.Table("chats").Column("teacher_last_read_at").Exists()) {
            Alter.Table("chats")
                .AddColumn("teacher_last_read_at").AsDateTime().Nullable();
        }
        if (!Schema.Table("chats").Column("parent_last_read_at").Exists()) {
            Alter.Table("chats")
                .AddColumn("parent_last_read_at").AsDateTime().Nullable();
        }
        if (!Schema.Table("chats").Column("student_last_read_at").Exists()) {
            Alter.Table("chats")
                .AddColumn("student_last_read_at").AsDateTime().Nullable();
        }
    }

    public override void Down() {
        if (Schema.Table("chats").Column("teacher_last_read_at").Exists()) {
            Delete.Column("teacher_last_read_at").FromTable("chats");
        }
        if (Schema.Table("chats").Column("parent_last_read_at").Exists()) {
            Delete.Column("parent_last_read_at").FromTable("chats");
        }
        if (Schema.Table("chats").Column("student_last_read_at").Exists()) {
            Delete.Column("student_last_read_at").FromTable("chats");
        }
    }
}

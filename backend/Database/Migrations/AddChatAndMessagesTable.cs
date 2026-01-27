using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601270001)]
public class AddChatAndMessagesTable : Migration {
    public override void Up() {
        // Only create tables if they don't exist
        if (!Schema.Table("chats").Exists()) {
            // Create chats table
            Create.Table("chats")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("chat_type").AsInt16().NotNullable()
                .WithColumn("teacher_id").AsGuid().NotNullable()
                .WithColumn("parent_id").AsGuid().Nullable()
                .WithColumn("student_id").AsGuid().Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true);

            // Create indexes on chats
            Create.Index("IX_chats_teacher_id")
                .OnTable("chats")
                .OnColumn("teacher_id");

            Create.Index("IX_chats_parent_id")
                .OnTable("chats")
                .OnColumn("parent_id");

            Create.Index("IX_chats_student_id")
                .OnTable("chats")
                .OnColumn("student_id");

            Create.Index("IX_chats_teacher_parent")
                .OnTable("chats")
                .OnColumn("teacher_id")
                .Ascending()
                .WithOptions()
                .NonClustered();

            Create.Index("IX_chats_teacher_student")
                .OnTable("chats")
                .OnColumn("teacher_id")
                .Ascending()
                .OnColumn("student_id")
                .Ascending()
                .WithOptions()
                .NonClustered();

            // Create foreign keys for chats
            Create.ForeignKey("FK_chats_teachers")
                .FromTable("chats").ForeignColumn("teacher_id")
                .ToTable("teachers").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_chats_parents")
                .FromTable("chats").ForeignColumn("parent_id")
                .ToTable("parents").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_chats_students")
                .FromTable("chats").ForeignColumn("student_id")
                .ToTable("students").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);
        }

        // Create messages table if not exists
        if (!Schema.Table("messages").Exists()) {
            Create.Table("messages")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("chat_id").AsGuid().NotNullable()
                .WithColumn("sender_id").AsGuid().NotNullable()
                .WithColumn("content").AsCustom("text").NotNullable()
                .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

            // Create indexes on messages
            Create.Index("IX_messages_chat_id")
                .OnTable("messages")
                .OnColumn("chat_id");

            Create.Index("IX_messages_sender_id")
                .OnTable("messages")
                .OnColumn("sender_id");

            Create.Index("IX_messages_created_at")
                .OnTable("messages")
                .OnColumn("created_at")
                .Descending();

            // Create foreign keys for messages
            Create.ForeignKey("FK_messages_chats")
                .FromTable("messages").ForeignColumn("chat_id")
                .ToTable("chats").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_messages_users")
                .FromTable("messages").ForeignColumn("sender_id")
                .ToTable("users").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.None);
        }

        // Create chat_attachments table if not exists
        if (!Schema.Table("chat_attachments").Exists()) {
            Create.Table("chat_attachments")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("message_id").AsGuid().Nullable()
                .WithColumn("chat_id").AsGuid().Nullable()
                .WithColumn("file_name").AsString(500).NotNullable()
                .WithColumn("file_url").AsString(1000).NotNullable()
                .WithColumn("file_size").AsInt64().NotNullable()
                .WithColumn("file_type").AsString(100).NotNullable()
                .WithColumn("uploaded_by").AsGuid().NotNullable()
                .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

            // Create indexes on chat_attachments
            Create.Index("IX_chat_attachments_chat_id")
                .OnTable("chat_attachments")
                .OnColumn("chat_id");

            Create.Index("IX_chat_attachments_message_id")
                .OnTable("chat_attachments")
                .OnColumn("message_id");

            Create.Index("IX_chat_attachments_uploaded_by")
                .OnTable("chat_attachments")
                .OnColumn("uploaded_by");

            // Create foreign keys for chat_attachments
            Create.ForeignKey("FK_chat_attachments_messages")
                .FromTable("chat_attachments").ForeignColumn("message_id")
                .ToTable("messages").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_chat_attachments_chats")
                .FromTable("chat_attachments").ForeignColumn("chat_id")
                .ToTable("chats").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_chat_attachments_users")
                .FromTable("chat_attachments").ForeignColumn("uploaded_by")
                .ToTable("users").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.None);
        }
    }

    public override void Down() {
        // Drop tables if they exist (safe for rollback)
        if (Schema.Table("chat_attachments").Exists()) {
            Delete.Table("chat_attachments");
        }
        if (Schema.Table("messages").Exists()) {
            Delete.Table("messages");
        }
        if (Schema.Table("chats").Exists()) {
            Delete.Table("chats");
        }
    }
}

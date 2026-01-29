using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601290001)]
public class AddTeacherDocuments : Migration {
    public override void Up() {
        // Create teacher_documents table
        Create.Table("teacher_documents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("teacher_id").AsGuid().NotNullable()
            .WithColumn("document_type").AsInt32().NotNullable()
            .WithColumn("file_path").AsString(500).NotNullable()
            .WithColumn("file_name").AsString(255).NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("rejection_reason").AsString().Nullable()
            .WithColumn("uploaded_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("reviewed_at").AsDateTime().Nullable()
            .WithColumn("reviewed_by").AsGuid().Nullable();

        Create.ForeignKey("FK_teacher_documents_teachers")
            .FromTable("teacher_documents").ForeignColumn("teacher_id")
            .ToTable("teachers").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_teacher_documents_teacher_id")
            .OnTable("teacher_documents")
            .OnColumn("teacher_id");

        Create.Index("IX_teacher_documents_teacher_id_document_type")
            .OnTable("teacher_documents")
            .OnColumn("teacher_id")
            .Ascending()
            .OnColumn("document_type")
            .Ascending();
    }

    public override void Down() {
        Delete.Table("teacher_documents");
    }
}

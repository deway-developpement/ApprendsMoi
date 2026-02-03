using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601290002)]
public class MoveTeacherDocumentsToDatabase : Migration {
    public override void Up() {
        // Add file_content column to store files in database
        Alter.Table("teacher_documents")
            .AddColumn("file_content").AsBinary().NotNullable().WithDefaultValue(new byte[] { });
        
        // Drop file_path column as we no longer need it
        Delete.Column("file_path").FromTable("teacher_documents");
    }

    public override void Down() {
        // Restore file_path column with a default value
        Alter.Table("teacher_documents")
            .AddColumn("file_path").AsString(500).NotNullable().WithDefaultValue("");
        
        // Remove file_content column
        Delete.Column("file_content").FromTable("teacher_documents");
    }
}

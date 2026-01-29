using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(202601290002)]
public class MoveTeacherDocumentsToDatabase : Migration {
    public override void Up() {
        // Add file_content column to store files in database
        // NOTE: Initially nullable to preserve existing records that still reference file_path
        // A separate data migration step is required to:
        // 1. Read files from disk using file_path values
        // 2. Populate file_content column with binary data
        // 3. Update records to have non-null file_content
        // 4. Only after all data is migrated should file_path be dropped
        Alter.Table("teacher_documents")
            .AddColumn("file_content").AsBinary().Nullable();
    }

    public override void Down() {
        // Remove file_content column
        Delete.Column("file_content").FromTable("teacher_documents");
    }
}

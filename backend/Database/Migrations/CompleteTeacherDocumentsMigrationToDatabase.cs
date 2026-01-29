using FluentMigrator;

namespace backend.Database.Migrations;

/// <summary>
/// FINAL CLEANUP: Complete the migration to database-stored documents
/// 
/// PREREQUISITES:
/// - All teacher_documents records must have file_content populated (not null)
/// - file_path values should no longer be used anywhere in the application
/// - All new uploads must be saving to file_content column
/// 
/// This migration:
/// 1. Makes file_content column NotNullable (enforces data integrity)
/// 2. Drops file_path column (removes file system dependencies)
/// 
/// If this migration fails with "Not null constraint violation", it means
/// there are still records with null file_content. Complete the data migration
/// before running this cleanup.
/// </summary>
[Migration(202601290004)]
public class CompleteTeacherDocumentsMigrationToDatabase : Migration {
    public override void Up() {
        // Make file_content NotNullable now that all data is migrated
        Alter.Table("teacher_documents")
            .AlterColumn("file_content").AsBinary().NotNullable();
        
        // Drop file_path column as all data has been migrated to file_content
        Delete.Column("file_path").FromTable("teacher_documents");
    }

    public override void Down() {
        // Restore file_path column for rollback
        Alter.Table("teacher_documents")
            .AddColumn("file_path").AsString(500).Nullable();
        
        // Make file_content nullable for rollback
        Alter.Table("teacher_documents")
            .AlterColumn("file_content").AsBinary().Nullable();
    }
}

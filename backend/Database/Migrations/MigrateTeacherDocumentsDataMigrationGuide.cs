using FluentMigrator;

namespace backend.Database.Migrations;

/// <summary>
/// DATA MIGRATION GUIDE for MoveTeacherDocumentsToDatabase (202601290002)
/// 
/// This is a guide for migrating teacher documents from file system storage to database storage.
/// Execute the following steps AFTER running the MoveTeacherDocumentsToDatabase migration:
/// 
/// STEP 1: Create a data migration service that:
///   - Queries all teacher_documents records with non-null file_path
///   - For each record:
///     * Read the file from disk using file_path
///     * Update file_content with the binary data
///     * Mark the record as migrated
/// 
/// STEP 2: Create a new migration (after all data is migrated) to:
///   - Make file_content column NotNullable
///   - Drop file_path column
/// 
/// STEP 3: Ensure no uploads use file_path after this migration
/// 
/// Current state (after 202601290002):
///   - file_content: Added as NULLABLE (allows existing records to remain valid)
///   - file_path: Still exists (backward compatible during migration period)
/// 
/// MIGRATION PROCEDURE:
/// 1. Deploy MoveTeacherDocumentsToDatabase migration
/// 2. Run application startup (seeds test data if needed)
/// 3. Execute data migration service to populate file_content from file_path
/// 4. Verify all records have file_content populated
/// 5. Deploy final cleanup migration to drop file_path and make file_content NotNullable
/// 
/// This two-step approach ensures:
///   - No data loss during migration
///   - Existing documents remain accessible during transition
///   - Graceful migration from file system to database storage
/// </summary>
[Migration(202601290003)]
public class MigrateTeacherDocumentsDataMigrationGuide : Migration {
    public override void Up() {
        // This migration is a placeholder for the data migration process
        // The actual data migration should be performed by:
        // 1. A service that reads files from disk and populates file_content
        // 2. Or manually executing SQL to verify the data migration
        
        // Example SQL to verify migration status:
        // SELECT id, file_path, file_content FROM teacher_documents
        // WHERE file_content IS NULL AND file_path IS NOT NULL
        
        // Once all data is migrated, proceed with cleanup migration
    }

    public override void Down() {
        // No-op for data migration guide
    }
}

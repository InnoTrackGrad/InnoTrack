using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnoTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithAzureDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('SimilarProjects', 'ProjectId') IS NULL
                    ALTER TABLE [SimilarProjects] ADD [ProjectId] int NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Projects', 'AcademicYearId1') IS NULL
                    ALTER TABLE [Projects] ADD [AcademicYearId1] int NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Projects', 'StudentNames') IS NULL
                    ALTER TABLE [Projects] ADD [StudentNames] nvarchar(max) NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Projects', 'Year') IS NULL
                    ALTER TABLE [Projects] ADD [Year] int NULL;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_SimilarProjects_ProjectId'
                        AND [object_id] = OBJECT_ID(N'[SimilarProjects]')
                )
                    CREATE INDEX [IX_SimilarProjects_ProjectId] ON [SimilarProjects] ([ProjectId]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Projects_AcademicYearId1'
                        AND [object_id] = OBJECT_ID(N'[Projects]')
                )
                    CREATE INDEX [IX_Projects_AcademicYearId1] ON [Projects] ([AcademicYearId1]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_Projects_AcademicYears_AcademicYearId1'
                )
                    ALTER TABLE [Projects]
                    ADD CONSTRAINT [FK_Projects_AcademicYears_AcademicYearId1]
                    FOREIGN KEY ([AcademicYearId1]) REFERENCES [AcademicYears] ([Id]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_SimilarProjects_Projects_ProjectId'
                )
                    ALTER TABLE [SimilarProjects]
                    ADD CONSTRAINT [FK_SimilarProjects_Projects_ProjectId]
                    FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_Projects_AcademicYears_AcademicYearId1'
                )
                    ALTER TABLE [Projects] DROP CONSTRAINT [FK_Projects_AcademicYears_AcademicYearId1];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_SimilarProjects_Projects_ProjectId'
                )
                    ALTER TABLE [SimilarProjects] DROP CONSTRAINT [FK_SimilarProjects_Projects_ProjectId];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_SimilarProjects_ProjectId'
                        AND [object_id] = OBJECT_ID(N'[SimilarProjects]')
                )
                    DROP INDEX [IX_SimilarProjects_ProjectId] ON [SimilarProjects];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Projects_AcademicYearId1'
                        AND [object_id] = OBJECT_ID(N'[Projects]')
                )
                    DROP INDEX [IX_Projects_AcademicYearId1] ON [Projects];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('SimilarProjects', 'ProjectId') IS NOT NULL
                    ALTER TABLE [SimilarProjects] DROP COLUMN [ProjectId];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Projects', 'AcademicYearId1') IS NOT NULL
                    ALTER TABLE [Projects] DROP COLUMN [AcademicYearId1];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Projects', 'StudentNames') IS NOT NULL
                    ALTER TABLE [Projects] DROP COLUMN [StudentNames];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Projects', 'Year') IS NOT NULL
                    ALTER TABLE [Projects] DROP COLUMN [Year];
                """);
        }
    }
}

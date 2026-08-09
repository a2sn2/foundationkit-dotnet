using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260809070000_AddCaseAttachments")]
public sealed class AddCaseAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseAttachments",
            schema: "madar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                StorageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseAttachments", item => item.Id);
                table.ForeignKey(
                    name: "FK_CaseAttachments_Cases_CaseId",
                    column: item => item.CaseId,
                    principalSchema: "madar",
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CaseAttachments_Users_UploadedByUserId",
                    column: item => item.UploadedByUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CaseAttachments_CaseId_CreatedUtc_Id",
            schema: "madar",
            table: "CaseAttachments",
            columns: new[] { "CaseId", "CreatedUtc", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_CaseAttachments_StorageKey",
            schema: "madar",
            table: "CaseAttachments",
            column: "StorageKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CaseAttachments_UploadedByUserId",
            schema: "madar",
            table: "CaseAttachments",
            column: "UploadedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseAttachments",
            schema: "madar");
    }
}

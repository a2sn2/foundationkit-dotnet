using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoundationKit.Workbench.Infrastructure.Migrations;

[DbContext(typeof(WorkbenchDbContext))]
[Migration("20260810204500_DurableIdempotency")]
public partial class DurableIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FoundationIdempotencyEntries",
            columns: table => new
            {
                ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                OperationScope = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                KeyHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                RequestFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                State = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                AcquiredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReplayUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                ResponseContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ResponseBody = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                ResponseLocation = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                ResponseEntityTag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_FoundationIdempotencyEntries",
                    x => new { x.ProjectId, x.OperationScope, x.KeyHash });
            });

        migrationBuilder.CreateIndex(
            name: "IX_FoundationIdempotencyEntries_ReplayUntilUtc",
            table: "FoundationIdempotencyEntries",
            column: "ReplayUntilUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "FoundationIdempotencyEntries");
}

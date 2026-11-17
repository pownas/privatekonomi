using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privatekonomi.Core.Migrations;

/// <inheritdoc />
public partial class AddHouseholdActivityImages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HouseholdActivityImages",
            columns: table => new
            {
                HouseholdActivityImageId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HouseholdActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Caption = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                MimeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                UploadedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HouseholdActivityImages", x => x.HouseholdActivityImageId);
                table.ForeignKey(
                    name: "FK_HouseholdActivityImages_HouseholdActivities_HouseholdActivityId",
                    column: x => x.HouseholdActivityId,
                    principalTable: "HouseholdActivities",
                    principalColumn: "HouseholdActivityId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdActivityImages_HouseholdActivityId",
            table: "HouseholdActivityImages",
            column: "HouseholdActivityId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HouseholdActivityImages");
    }
}

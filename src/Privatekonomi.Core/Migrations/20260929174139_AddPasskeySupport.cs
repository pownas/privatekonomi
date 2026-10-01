using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

#nullable disable

namespace Privatekonomi.Core.Migrations;

public partial class AddPasskeySupport : Migration
{
    internal static IReadOnlyList<MigrationOperation> GetUpOperations(string activeProvider)
    {
        var migrationBuilder = new MigrationBuilder(activeProvider);
        new AddPasskeySupport().Up(migrationBuilder);
        return migrationBuilder.Operations;
    }

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetUserPasskeys",
            columns: table => new
            {
                CredentialId = table.Column<byte[]>(maxLength: 1024, nullable: false),
                UserId = table.Column<string>(nullable: false),
                Data = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserPasskeys", x => x.CredentialId);
                table.ForeignKey(
                    name: "FK_AspNetUserPasskeys_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserPasskeys_UserId",
            table: "AspNetUserPasskeys",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AspNetUserPasskeys");
    }
}

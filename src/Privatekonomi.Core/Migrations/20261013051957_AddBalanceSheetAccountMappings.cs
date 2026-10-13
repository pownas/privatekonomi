using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privatekonomi.Core.Migrations;

public partial class AddBalanceSheetAccountMappings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ChartOfAccountsCode",
            table: "Loans",
            type: "TEXT",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ChartOfAccountsCode",
            table: "Investments",
            type: "TEXT",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ChartOfAccountsCode",
            table: "Assets",
            type: "TEXT",
            maxLength: 20,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ChartOfAccountsCode", table: "Loans");
        migrationBuilder.DropColumn(name: "ChartOfAccountsCode", table: "Investments");
        migrationBuilder.DropColumn(name: "ChartOfAccountsCode", table: "Assets");
    }
}

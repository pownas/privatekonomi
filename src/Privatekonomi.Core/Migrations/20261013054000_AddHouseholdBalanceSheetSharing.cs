using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Privatekonomi.Core.Data;

#nullable disable

namespace Privatekonomi.Core.Migrations;

[DbContext(typeof(PrivatekonomyContext))]
[Migration("20261013054000_AddHouseholdBalanceSheetSharing")]
public partial class AddHouseholdBalanceSheetSharing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ShareBalanceSheet",
            table: "Households",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ShareBalanceSheet",
            table: "Households");
    }
}

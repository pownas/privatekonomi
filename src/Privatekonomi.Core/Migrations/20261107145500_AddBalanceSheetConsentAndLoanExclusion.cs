using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Privatekonomi.Core.Data;

#nullable disable

namespace Privatekonomi.Core.Migrations;

[DbContext(typeof(PrivatekonomyContext))]
[Migration("20261107145500_AddBalanceSheetConsentAndLoanExclusion")]
public partial class AddBalanceSheetConsentAndLoanExclusion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ShareBalanceSheetConsent",
            table: "HouseholdMembers",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ExcludeFromBalanceSheet",
            table: "Loans",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("UPDATE Households SET ShareBalanceSheet = 0 WHERE ShareBalanceSheet <> 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ShareBalanceSheetConsent", table: "HouseholdMembers");
        migrationBuilder.DropColumn(name: "ExcludeFromBalanceSheet", table: "Loans");
    }
}

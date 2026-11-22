using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Privatekonomi.Core.Data;

#nullable disable

namespace Privatekonomi.Core.Migrations;

[DbContext(typeof(PrivatekonomyContext))]
[Migration("20261122173352_AddHouseholdToBankSource")]
public partial class AddHouseholdToBankSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "HouseholdId",
            table: "BankSources",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_BankSources_HouseholdId",
            table: "BankSources",
            column: "HouseholdId");

        migrationBuilder.AddForeignKey(
            name: "FK_BankSources_Households_HouseholdId",
            table: "BankSources",
            column: "HouseholdId",
            principalTable: "Households",
            principalColumn: "HouseholdId",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BankSources_Households_HouseholdId",
            table: "BankSources");

        migrationBuilder.DropIndex(
            name: "IX_BankSources_HouseholdId",
            table: "BankSources");

        migrationBuilder.DropColumn(
            name: "HouseholdId",
            table: "BankSources");
    }
}
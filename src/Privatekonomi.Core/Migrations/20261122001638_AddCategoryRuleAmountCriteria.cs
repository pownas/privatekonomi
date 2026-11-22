using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privatekonomi.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryRuleAmountCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExactAmount",
                table: "CategoryRules",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumAmount",
                table: "CategoryRules",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumAmount",
                table: "CategoryRules",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ExactAmount", table: "CategoryRules");
            migrationBuilder.DropColumn(name: "MinimumAmount", table: "CategoryRules");
            migrationBuilder.DropColumn(name: "MaximumAmount", table: "CategoryRules");
        }
    }
}

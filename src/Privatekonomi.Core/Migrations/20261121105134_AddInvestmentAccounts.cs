using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privatekonomi.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvestmentAccountId",
                table: "Investments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvestmentAccounts",
                columns: table => new
                {
                    InvestmentAccountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsFundRobot = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    InitialContributions = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CurrentValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentAccounts", x => x.InvestmentAccountId);
                    table.ForeignKey(
                        name: "FK_InvestmentAccounts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7416), new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7524) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7756), new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7757) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7759), new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7759) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7762), new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7762) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 5,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7765), new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7765) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 6,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7768), new DateTime(2026, 11, 21, 10, 51, 33, 798, DateTimeKind.Utc).AddTicks(7769) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8169));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8297));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8300));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8303));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8324));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8327));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8331));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8334));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8336));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8498));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8502));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8506));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8518));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8521));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8524));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8527));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8531));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8535));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8549));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8552));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8556));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8560));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8577));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8581));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8583));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8595));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8598));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8601));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8604));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8608));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8612));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8615));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2026, 11, 21, 10, 51, 33, 797, DateTimeKind.Utc).AddTicks(8627));

            migrationBuilder.CreateIndex(
                name: "IX_Investments_InvestmentAccountId",
                table: "Investments",
                column: "InvestmentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentAccounts_UserId",
                table: "InvestmentAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentAccounts_UserId_Provider_AccountNumber",
                table: "InvestmentAccounts",
                columns: new[] { "UserId", "Provider", "AccountNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_InvestmentAccounts_InvestmentAccountId",
                table: "Investments",
                column: "InvestmentAccountId",
                principalTable: "InvestmentAccounts",
                principalColumn: "InvestmentAccountId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Investments_InvestmentAccounts_InvestmentAccountId",
                table: "Investments");

            migrationBuilder.DropTable(
                name: "InvestmentAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Investments_InvestmentAccountId",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "InvestmentAccountId",
                table: "Investments");

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(3567), new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(3808) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4303), new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4304) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4307), new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4307) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4309), new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4310) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 5,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4312), new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4312) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 6,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4314), new DateTime(2026, 9, 29, 17, 41, 37, 740, DateTimeKind.Utc).AddTicks(4315) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3266));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3534));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3538));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3540));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3543));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3545));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3547));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3550));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3552));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3809));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3815));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3818));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3820));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3823));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3825));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3827));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3830));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3832));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3834));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3836));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3839));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3841));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3844));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3846));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3848));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3851));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3853));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3855));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3878));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3880));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3883));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3885));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3887));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 17, 41, 37, 739, DateTimeKind.Utc).AddTicks(3890));
        }
    }
}

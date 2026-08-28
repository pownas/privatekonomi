using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privatekonomi.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountNumberToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClearingNumber",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutoSavingsType",
                table: "Goals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAutoSavingsAmount",
                table: "Goals",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "BankSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChartOfAccountsCode",
                table: "BankSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClearingNumber",
                table: "BankSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAdmin",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillSchedules",
                columns: table => new
                {
                    BillScheduleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BillId = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextOccurrenceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DaysBeforeCreate = table.Column<int>(type: "INTEGER", nullable: false),
                    ReminderDaysBefore = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillSchedules", x => x.BillScheduleId);
                    table.ForeignKey(
                        name: "FK_BillSchedules_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetSuggestions",
                columns: table => new
                {
                    BudgetSuggestionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TotalIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DistributionModel = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAccepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AppliedBudgetId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    HouseholdId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetSuggestions", x => x.BudgetSuggestionId);
                    table.ForeignKey(
                        name: "FK_BudgetSuggestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetSuggestions_Budgets_AppliedBudgetId",
                        column: x => x.AppliedBudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BudgetSuggestions_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "HouseholdId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DashboardLayouts",
                columns: table => new
                {
                    LayoutId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardLayouts", x => x.LayoutId);
                    table.ForeignKey(
                        name: "FK_DashboardLayouts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebtSettlements",
                columns: table => new
                {
                    DebtSettlementId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                    DebtorMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreditorMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SettledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SettlementNote = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtSettlements", x => x.DebtSettlementId);
                    table.ForeignKey(
                        name: "FK_DebtSettlements_HouseholdMembers_CreditorMemberId",
                        column: x => x.CreditorMemberId,
                        principalTable: "HouseholdMembers",
                        principalColumn: "HouseholdMemberId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebtSettlements_HouseholdMembers_DebtorMemberId",
                        column: x => x.DebtorMemberId,
                        principalTable: "HouseholdMembers",
                        principalColumn: "HouseholdMemberId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebtSettlements_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "HouseholdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdActivities",
                columns: table => new
                {
                    HouseholdActivityId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedByMemberId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdActivities", x => x.HouseholdActivityId);
                    table.ForeignKey(
                        name: "FK_HouseholdActivities_HouseholdMembers_CompletedByMemberId",
                        column: x => x.CompletedByMemberId,
                        principalTable: "HouseholdMembers",
                        principalColumn: "HouseholdMemberId");
                    table.ForeignKey(
                        name: "FK_HouseholdActivities_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "HouseholdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdBudgetShares",
                columns: table => new
                {
                    HouseholdBudgetShareId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BudgetId = table.Column<int>(type: "INTEGER", nullable: false),
                    HouseholdMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    SharePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    FixedContribution = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdBudgetShares", x => x.HouseholdBudgetShareId);
                    table.ForeignKey(
                        name: "FK_HouseholdBudgetShares_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HouseholdBudgetShares_HouseholdMembers_HouseholdMemberId",
                        column: x => x.HouseholdMemberId,
                        principalTable: "HouseholdMembers",
                        principalColumn: "HouseholdMemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdTasks",
                columns: table => new
                {
                    HouseholdTaskId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "INTEGER", nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "INTEGER", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ParentTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssignedToMemberId = table.Column<int>(type: "INTEGER", nullable: true),
                    CompletedByMemberId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdTasks", x => x.HouseholdTaskId);
                    table.ForeignKey(
                        name: "FK_HouseholdTasks_HouseholdMembers_AssignedToMemberId",
                        column: x => x.AssignedToMemberId,
                        principalTable: "HouseholdMembers",
                        principalColumn: "HouseholdMemberId");
                    table.ForeignKey(
                        name: "FK_HouseholdTasks_HouseholdMembers_CompletedByMemberId",
                        column: x => x.CompletedByMemberId,
                        principalTable: "HouseholdMembers",
                        principalColumn: "HouseholdMemberId");
                    table.ForeignKey(
                        name: "FK_HouseholdTasks_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "HouseholdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    ImportJobId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalRows = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DuplicateCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessages = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.ImportJobId);
                    table.ForeignKey(
                        name: "FK_ImportJobs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyReports",
                columns: table => new
                {
                    MonthlyReportId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportMonth = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalExpenses = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NetFlow = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IncomeChangePercent = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    ExpenseChangePercent = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    CategorySummariesJson = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    TopMerchantsJson = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    BudgetOutcomeJson = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    InsightsJson = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    HouseholdId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyReports", x => x.MonthlyReportId);
                    table.ForeignKey(
                        name: "FK_MonthlyReports_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyReports_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "HouseholdId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReportPreferences",
                columns: table => new
                {
                    ReportPreferenceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SendEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowInApp = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PreferredDeliveryDay = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludeCategoryDetails = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeTopMerchants = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeBudgetComparison = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeTrendAnalysis = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPreferences", x => x.ReportPreferenceId);
                    table.ForeignKey(
                        name: "FK_ReportPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetAdjustments",
                columns: table => new
                {
                    BudgetAdjustmentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BudgetSuggestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NewAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AdjustedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TransferToCategoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAdjustments", x => x.BudgetAdjustmentId);
                    table.ForeignKey(
                        name: "FK_BudgetAdjustments_BudgetSuggestions_BudgetSuggestionId",
                        column: x => x.BudgetSuggestionId,
                        principalTable: "BudgetSuggestions",
                        principalColumn: "BudgetSuggestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetAdjustments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetAdjustments_Categories_TransferToCategoryId",
                        column: x => x.TransferToCategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetSuggestionItems",
                columns: table => new
                {
                    BudgetSuggestionItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BudgetSuggestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    SuggestedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AdjustedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    AllocationCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    RecurrencePeriodMonths = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsManuallyAdjusted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetSuggestionItems", x => x.BudgetSuggestionItemId);
                    table.ForeignKey(
                        name: "FK_BudgetSuggestionItems_BudgetSuggestions_BudgetSuggestionId",
                        column: x => x.BudgetSuggestionId,
                        principalTable: "BudgetSuggestions",
                        principalColumn: "BudgetSuggestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetSuggestionItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WidgetConfigurations",
                columns: table => new
                {
                    WidgetConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Row = table.Column<int>(type: "INTEGER", nullable: false),
                    Column = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Settings = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetConfigurations", x => x.WidgetConfigId);
                    table.ForeignKey(
                        name: "FK_WidgetConfigurations_DashboardLayouts_LayoutId",
                        column: x => x.LayoutId,
                        principalTable: "DashboardLayouts",
                        principalColumn: "LayoutId",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "ReportDeliveries",
                columns: table => new
                {
                    ReportDeliveryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MonthlyReportId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDeliveries", x => x.ReportDeliveryId);
                    table.ForeignKey(
                        name: "FK_ReportDeliveries_MonthlyReports_MonthlyReportId",
                        column: x => x.MonthlyReportId,
                        principalTable: "MonthlyReports",
                        principalColumn: "MonthlyReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 1,
                columns: new[] { "AccountNumber", "ChartOfAccountsCode", "ClearingNumber", "CreatedAt", "ValidFrom" },
                values: new object[] { null, null, null, new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2200), new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2421) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 2,
                columns: new[] { "AccountNumber", "ChartOfAccountsCode", "ClearingNumber", "CreatedAt", "ValidFrom" },
                values: new object[] { null, null, null, new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2877), new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2877) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 3,
                columns: new[] { "AccountNumber", "ChartOfAccountsCode", "ClearingNumber", "CreatedAt", "ValidFrom" },
                values: new object[] { null, null, null, new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2880), new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2880) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 4,
                columns: new[] { "AccountNumber", "ChartOfAccountsCode", "ClearingNumber", "CreatedAt", "ValidFrom" },
                values: new object[] { null, null, null, new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2882), new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2883) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 5,
                columns: new[] { "AccountNumber", "ChartOfAccountsCode", "ClearingNumber", "CreatedAt", "ValidFrom" },
                values: new object[] { null, null, null, new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2884), new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2885) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 6,
                columns: new[] { "AccountNumber", "ChartOfAccountsCode", "ClearingNumber", "CreatedAt", "ValidFrom" },
                values: new object[] { null, null, null, new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2887), new DateTime(2026, 3, 2, 21, 19, 58, 115, DateTimeKind.Utc).AddTicks(2887) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3059));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3466));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3469));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3471));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3474));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3476));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3478));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3480));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3482));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3756));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3761));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3764));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3766));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3768));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3771));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3773));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3775));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3777));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3780));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3782));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3784));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3787));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3789));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3791));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3793));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3796));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3798));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3800));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3802));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3821));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3823));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3826));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3828));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 21, 19, 58, 114, DateTimeKind.Utc).AddTicks(3830));

            migrationBuilder.CreateIndex(
                name: "IX_BillSchedules_BillId",
                table: "BillSchedules",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BillSchedules_IsActive",
                table: "BillSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BillSchedules_NextOccurrenceDate",
                table: "BillSchedules",
                column: "NextOccurrenceDate");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAdjustments_BudgetSuggestionId",
                table: "BudgetAdjustments",
                column: "BudgetSuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAdjustments_CategoryId",
                table: "BudgetAdjustments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAdjustments_TransferToCategoryId",
                table: "BudgetAdjustments",
                column: "TransferToCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSuggestionItems_BudgetSuggestionId",
                table: "BudgetSuggestionItems",
                column: "BudgetSuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSuggestionItems_CategoryId",
                table: "BudgetSuggestionItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSuggestions_AppliedBudgetId",
                table: "BudgetSuggestions",
                column: "AppliedBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSuggestions_HouseholdId",
                table: "BudgetSuggestions",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSuggestions_IsAccepted",
                table: "BudgetSuggestions",
                column: "IsAccepted");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSuggestions_UserId",
                table: "BudgetSuggestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardLayouts_UserId",
                table: "DashboardLayouts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardLayouts_UserId_IsDefault",
                table: "DashboardLayouts",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_DebtSettlements_CreditorMemberId",
                table: "DebtSettlements",
                column: "CreditorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtSettlements_DebtorMemberId",
                table: "DebtSettlements",
                column: "DebtorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtSettlements_HouseholdId",
                table: "DebtSettlements",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdActivities_CompletedByMemberId",
                table: "HouseholdActivities",
                column: "CompletedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdActivities_HouseholdId",
                table: "HouseholdActivities",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdActivityImages_HouseholdActivityId",
                table: "HouseholdActivityImages",
                column: "HouseholdActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdBudgetShares_BudgetId",
                table: "HouseholdBudgetShares",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdBudgetShares_HouseholdMemberId",
                table: "HouseholdBudgetShares",
                column: "HouseholdMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdTasks_AssignedToMemberId",
                table: "HouseholdTasks",
                column: "AssignedToMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdTasks_CompletedByMemberId",
                table: "HouseholdTasks",
                column: "CompletedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdTasks_HouseholdId",
                table: "HouseholdTasks",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedAt",
                table: "ImportJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_UserId",
                table: "ImportJobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_UserId_Status",
                table: "ImportJobs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_HouseholdId",
                table: "MonthlyReports",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_ReportMonth",
                table: "MonthlyReports",
                column: "ReportMonth");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_UserId",
                table: "MonthlyReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_UserId_ReportMonth",
                table: "MonthlyReports",
                columns: new[] { "UserId", "ReportMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_ValidFrom",
                table: "MonthlyReports",
                column: "ValidFrom");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_ValidTo",
                table: "MonthlyReports",
                column: "ValidTo");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDeliveries_MonthlyReportId",
                table: "ReportDeliveries",
                column: "MonthlyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDeliveries_ScheduledAt",
                table: "ReportDeliveries",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDeliveries_Status",
                table: "ReportDeliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPreferences_UserId",
                table: "ReportPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WidgetConfigurations_LayoutId",
                table: "WidgetConfigurations",
                column: "LayoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillSchedules");

            migrationBuilder.DropTable(
                name: "BudgetAdjustments");

            migrationBuilder.DropTable(
                name: "BudgetSuggestionItems");

            migrationBuilder.DropTable(
                name: "DebtSettlements");

            migrationBuilder.DropTable(
                name: "HouseholdActivityImages");

            migrationBuilder.DropTable(
                name: "HouseholdBudgetShares");

            migrationBuilder.DropTable(
                name: "HouseholdTasks");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "ReportDeliveries");

            migrationBuilder.DropTable(
                name: "ReportPreferences");

            migrationBuilder.DropTable(
                name: "WidgetConfigurations");

            migrationBuilder.DropTable(
                name: "BudgetSuggestions");

            migrationBuilder.DropTable(
                name: "HouseholdActivities");

            migrationBuilder.DropTable(
                name: "MonthlyReports");

            migrationBuilder.DropTable(
                name: "DashboardLayouts");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ClearingNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AutoSavingsType",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "MonthlyAutoSavingsAmount",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "BankSources");

            migrationBuilder.DropColumn(
                name: "ChartOfAccountsCode",
                table: "BankSources");

            migrationBuilder.DropColumn(
                name: "ClearingNumber",
                table: "BankSources");

            migrationBuilder.DropColumn(
                name: "IsSystemAdmin",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7033), new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7225) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7635), new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7636) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7638), new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7639) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7641), new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7641) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 5,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7644), new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7644) });

            migrationBuilder.UpdateData(
                table: "BankSources",
                keyColumn: "BankSourceId",
                keyValue: 6,
                columns: new[] { "CreatedAt", "ValidFrom" },
                values: new object[] { new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7646), new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7647) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8456));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8660));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8663));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8666));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8669));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8672));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8674));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8676));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8678));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9004));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9011));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9014));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9017));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9020));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9022));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9024));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9027));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9044));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9047));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9049));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9051));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9054));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9056));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9058));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9060));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9063));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9065));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9067));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9072));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9074));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9079));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9081));
        }
    }
}

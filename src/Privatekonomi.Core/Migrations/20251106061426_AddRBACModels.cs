using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Privatekonomi.Core.Migrations;

/// <inheritdoc />
public partial class AddRBACModels : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogEntries",
            columns: table => new
            {
                AuditLogEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                UserEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Category = table.Column<int>(type: "INTEGER", nullable: false),
                Severity = table.Column<int>(type: "INTEGER", nullable: false),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: true),
                ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ResourceId = table.Column<int>(type: "INTEGER", nullable: true),
                Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                OldValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                NewValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Success = table.Column<bool>(type: "INTEGER", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogEntries", x => x.AuditLogEntryId);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                AuditLogId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                EntityId = table.Column<int>(type: "INTEGER", nullable: true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
            });

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                DefaultBudgetMonthly = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                TaxRelated = table.Column<bool>(type: "INTEGER", nullable: false),
                IsSystemCategory = table.Column<bool>(type: "INTEGER", nullable: false),
                OriginalName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                OriginalColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                OriginalAccountNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.CategoryId);
                table.ForeignKey(
                    name: "FK_Categories_Categories_ParentId",
                    column: x => x.ParentId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ChallengeTemplates",
            columns: table => new
            {
                ChallengeTemplateId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Icon = table.Column<string>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                DurationDays = table.Column<int>(type: "INTEGER", nullable: false),
                Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                Category = table.Column<int>(type: "INTEGER", nullable: false),
                EstimatedSavingsMin = table.Column<decimal>(type: "TEXT", nullable: true),
                EstimatedSavingsMax = table.Column<decimal>(type: "TEXT", nullable: true),
                SuggestedTargetAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                Rules = table.Column<string>(type: "TEXT", nullable: true),
                Tags = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChallengeTemplates", x => x.ChallengeTemplateId);
            });

        migrationBuilder.CreateTable(
            name: "CommuteDeductions",
            columns: table => new
            {
                CommuteDeductionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                FromAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ToAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                DistanceKm = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                NumberOfTrips = table.Column<int>(type: "INTEGER", nullable: false),
                TransportMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Cost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                DeductibleAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TaxYear = table.Column<int>(type: "INTEGER", nullable: false),
                IsRegularCommute = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommuteDeductions", x => x.CommuteDeductionId);
            });

        migrationBuilder.CreateTable(
            name: "Households",
            columns: table => new
            {
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Households", x => x.HouseholdId);
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RolePermissionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleType = table.Column<int>(type: "INTEGER", nullable: false),
                PermissionKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                IsAllowed = table.Column<bool>(type: "INTEGER", nullable: false),
                AmountLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionId);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CreditRatings",
            columns: table => new
            {
                CreditRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Rating = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: true),
                CheckedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                PaymentRemarks = table.Column<int>(type: "INTEGER", nullable: false),
                TotalDebt = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                CreditUtilization = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CreditRatings", x => x.CreditRatingId);
                table.ForeignKey(
                    name: "FK_CreditRatings_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "HouseholdId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HouseholdMembers",
            columns: table => new
            {
                HouseholdMemberId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                JoinedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LeftDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true),
                DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HouseholdMembers", x => x.HouseholdMemberId);
                table.ForeignKey(
                    name: "FK_HouseholdMembers_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "HouseholdId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedExpenses",
            columns: table => new
            {
                SharedExpenseId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                ExpenseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                SplitMethod = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedExpenses", x => x.SharedExpenseId);
                table.ForeignKey(
                    name: "FK_SharedExpenses_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "HouseholdId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                HouseholdMemberId = table.Column<int>(type: "INTEGER", nullable: true),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUsers_HouseholdMembers_HouseholdMemberId",
                    column: x => x.HouseholdMemberId,
                    principalTable: "HouseholdMembers",
                    principalColumn: "HouseholdMemberId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ChildAllowances",
            columns: table => new
            {
                ChildAllowanceId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HouseholdMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChildAllowances", x => x.ChildAllowanceId);
                table.ForeignKey(
                    name: "FK_ChildAllowances_HouseholdMembers_HouseholdMemberId",
                    column: x => x.HouseholdMemberId,
                    principalTable: "HouseholdMembers",
                    principalColumn: "HouseholdMemberId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HouseholdRoles",
            columns: table => new
            {
                HouseholdRoleId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HouseholdMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                RoleType = table.Column<int>(type: "INTEGER", nullable: false),
                IsDelegated = table.Column<bool>(type: "INTEGER", nullable: false),
                DelegatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                DelegationStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                DelegationEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                AssignedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                RevokedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                RevokedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HouseholdRoles", x => x.HouseholdRoleId);
                table.ForeignKey(
                    name: "FK_HouseholdRoles_HouseholdMembers_HouseholdMemberId",
                    column: x => x.HouseholdMemberId,
                    principalTable: "HouseholdMembers",
                    principalColumn: "HouseholdMemberId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ExpenseShares",
            columns: table => new
            {
                ExpenseShareId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SharedExpenseId = table.Column<int>(type: "INTEGER", nullable: false),
                HouseholdMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                ShareAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                SharePercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                RoomSize = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseShares", x => x.ExpenseShareId);
                table.ForeignKey(
                    name: "FK_ExpenseShares_HouseholdMembers_HouseholdMemberId",
                    column: x => x.HouseholdMemberId,
                    principalTable: "HouseholdMembers",
                    principalColumn: "HouseholdMemberId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExpenseShares_SharedExpenses_SharedExpenseId",
                    column: x => x.SharedExpenseId,
                    principalTable: "SharedExpenses",
                    principalColumn: "SharedExpenseId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                RoleId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Assets",
            columns: table => new
            {
                AssetId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                PurchaseValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Assets", x => x.AssetId);
                table.ForeignKey(
                    name: "FK_Assets_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BankSources",
            columns: table => new
            {
                BankSourceId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                Logo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                AccountType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                Institution = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                InitialBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                OpenedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ClosedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BankSources", x => x.BankSourceId);
                table.ForeignKey(
                    name: "FK_BankSources_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BudgetAlertSettings",
            columns: table => new
            {
                BudgetAlertSettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                EnableAlert75 = table.Column<bool>(type: "INTEGER", nullable: false),
                EnableAlert90 = table.Column<bool>(type: "INTEGER", nullable: false),
                EnableAlert100 = table.Column<bool>(type: "INTEGER", nullable: false),
                CustomThresholds = table.Column<string>(type: "TEXT", nullable: true),
                EnableForecastWarnings = table.Column<bool>(type: "INTEGER", nullable: false),
                ForecastWarningDays = table.Column<int>(type: "INTEGER", nullable: false),
                EnableBudgetFreeze = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BudgetAlertSettings", x => x.BudgetAlertSettingsId);
                table.ForeignKey(
                    name: "FK_BudgetAlertSettings_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Budgets",
            columns: table => new
            {
                BudgetId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Period = table.Column<int>(type: "INTEGER", nullable: false),
                TemplateType = table.Column<int>(type: "INTEGER", nullable: true),
                RolloverUnspent = table.Column<bool>(type: "INTEGER", nullable: false),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Budgets", x => x.BudgetId);
                table.ForeignKey(
                    name: "FK_Budgets_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Budgets_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "HouseholdId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "CategoryRules",
            columns: table => new
            {
                CategoryRuleId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Pattern = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CaseSensitive = table.Column<bool>(type: "INTEGER", nullable: false),
                Field = table.Column<int>(type: "INTEGER", nullable: false),
                RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                OverridesSystemRuleId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryRules", x => x.CategoryRuleId);
                table.ForeignKey(
                    name: "FK_CategoryRules_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryRules_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryRules_CategoryRules_OverridesSystemRuleId",
                    column: x => x.OverridesSystemRuleId,
                    principalTable: "CategoryRules",
                    principalColumn: "CategoryRuleId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CurrencyAccounts",
            columns: table => new
            {
                CurrencyAccountId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ExchangeRateUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CurrencyAccounts", x => x.CurrencyAccountId);
                table.ForeignKey(
                    name: "FK_CurrencyAccounts_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DoNotDisturbSchedules",
            columns: table => new
            {
                DoNotDisturbScheduleId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                StartTime = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                EndTime = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowCritical = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoNotDisturbSchedules", x => x.DoNotDisturbScheduleId);
                table.ForeignKey(
                    name: "FK_DoNotDisturbSchedules_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LifeTimelineMilestones",
            columns: table => new
            {
                LifeTimelineMilestoneId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                MilestoneType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                PlannedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                EstimatedCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                RequiredMonthlySavings = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                ProgressPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                CurrentSavings = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LifeTimelineMilestones", x => x.LifeTimelineMilestoneId);
                table.ForeignKey(
                    name: "FK_LifeTimelineMilestones_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LifeTimelineScenarios",
            columns: table => new
            {
                LifeTimelineScenarioId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ExpectedReturnRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                MonthlySavings = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                RetirementAge = table.Column<int>(type: "INTEGER", nullable: false),
                ExpectedMonthlyPension = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ProjectedRetirementWealth = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                InflationRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                SalaryIncreaseRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                IsBaseline = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LifeTimelineScenarios", x => x.LifeTimelineScenarioId);
                table.ForeignKey(
                    name: "FK_LifeTimelineScenarios_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Loans",
            columns: table => new
            {
                LoanId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                InterestRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                Amortization = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                MaturityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                PropertyAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                PropertyValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                LoanProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                IsFixedRate = table.Column<bool>(type: "INTEGER", nullable: false),
                RateResetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                BindingPeriodMonths = table.Column<int>(type: "INTEGER", nullable: true),
                CSN_LoanType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CSN_StudyYear = table.Column<int>(type: "INTEGER", nullable: true),
                CSN_MonthlyPayment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                CSN_RemainingAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                CSN_LastUpdate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                MinimumPayment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                InstallmentMonths = table.Column<int>(type: "INTEGER", nullable: true),
                InstallmentFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                ExtraMonthlyPayment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Loans", x => x.LoanId);
                table.ForeignKey(
                    name: "FK_Loans_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MLModels",
            columns: table => new
            {
                ModelId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                ModelPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                TrainedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                TrainingRecordsCount = table.Column<int>(type: "INTEGER", nullable: false),
                Accuracy = table.Column<float>(type: "REAL", nullable: false),
                Precision = table.Column<float>(type: "REAL", nullable: false),
                Recall = table.Column<float>(type: "REAL", nullable: false),
                Metrics = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MLModels", x => x.ModelId);
                table.ForeignKey(
                    name: "FK_MLModels_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NetWorthSnapshots",
            columns: table => new
            {
                NetWorthSnapshotId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                TotalAssets = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TotalLiabilities = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                NetWorth = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                BankBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                InvestmentValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                PhysicalAssetValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                LoanBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                IsManual = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NetWorthSnapshots", x => x.NetWorthSnapshotId);
                table.ForeignKey(
                    name: "FK_NetWorthSnapshots_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NotificationIntegrations",
            columns: table => new
            {
                NotificationIntegrationId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Channel = table.Column<int>(type: "INTEGER", nullable: false),
                Configuration = table.Column<string>(type: "TEXT", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationIntegrations", x => x.NotificationIntegrationId);
                table.ForeignKey(
                    name: "FK_NotificationIntegrations_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NotificationPreferences",
            columns: table => new
            {
                NotificationPreferenceId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                NotificationType = table.Column<int>(type: "INTEGER", nullable: false),
                EnabledChannels = table.Column<int>(type: "INTEGER", nullable: false),
                MinimumPriority = table.Column<int>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                DigestMode = table.Column<bool>(type: "INTEGER", nullable: false),
                DigestIntervalHours = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationPreferences", x => x.NotificationPreferenceId);
                table.ForeignKey(
                    name: "FK_NotificationPreferences_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                NotificationId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Data = table.Column<string>(type: "TEXT", nullable: true),
                Channel = table.Column<int>(type: "INTEGER", nullable: false),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ActionUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                SnoozeUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                SnoozeCount = table.Column<int>(type: "INTEGER", nullable: false),
                BillReminderId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                table.ForeignKey(
                    name: "FK_Notifications_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Pensions",
            columns: table => new
            {
                PensionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                PensionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                CurrentValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TotalContributions = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                MonthlyContribution = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                ExpectedMonthlyPension = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                RetirementAge = table.Column<int>(type: "INTEGER", nullable: true),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pensions", x => x.PensionId);
                table.ForeignKey(
                    name: "FK_Pensions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PortfolioAllocations",
            columns: table => new
            {
                PortfolioAllocationId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                AssetClass = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                TargetPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                MinPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                MaxPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PortfolioAllocations", x => x.PortfolioAllocationId);
                table.ForeignKey(
                    name: "FK_PortfolioAllocations_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Reminders",
            columns: table => new
            {
                ReminderId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ReminderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                SnoozeUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                SnoozeCount = table.Column<int>(type: "INTEGER", nullable: false),
                CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                ReminderType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                RelatedEntityId = table.Column<int>(type: "INTEGER", nullable: true),
                RelatedEntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                EscalationLevel = table.Column<int>(type: "INTEGER", nullable: false),
                LastFollowUpDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                EnableFollowUp = table.Column<bool>(type: "INTEGER", nullable: false),
                FollowUpIntervalHours = table.Column<int>(type: "INTEGER", nullable: false),
                MaxFollowUps = table.Column<int>(type: "INTEGER", nullable: false),
                ActionUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Metadata = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reminders", x => x.ReminderId);
                table.ForeignKey(
                    name: "FK_Reminders_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReminderSettings",
            columns: table => new
            {
                ReminderSettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                EnableEscalation = table.Column<bool>(type: "INTEGER", nullable: false),
                SnoozeThresholdForEscalation = table.Column<int>(type: "INTEGER", nullable: false),
                EscalateToEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                EscalateToSMS = table.Column<bool>(type: "INTEGER", nullable: false),
                EscalateToPush = table.Column<bool>(type: "INTEGER", nullable: false),
                DefaultSnoozeDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                EnableFollowUp = table.Column<bool>(type: "INTEGER", nullable: false),
                FollowUpIntervalHours = table.Column<int>(type: "INTEGER", nullable: false),
                MaxFollowUps = table.Column<int>(type: "INTEGER", nullable: false),
                QuietHoursStart = table.Column<int>(type: "INTEGER", nullable: false),
                QuietHoursEnd = table.Column<int>(type: "INTEGER", nullable: false),
                RespectQuietHours = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReminderSettings", x => x.ReminderSettingsId);
                table.ForeignKey(
                    name: "FK_ReminderSettings_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SalaryHistories",
            columns: table => new
            {
                SalaryHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                MonthlySalary = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                JobTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Employer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                EmploymentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                WorkPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SalaryHistories", x => x.SalaryHistoryId);
                table.ForeignKey(
                    name: "FK_SalaryHistories_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SavingsChallenges",
            columns: table => new
            {
                SavingsChallengeId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                TargetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                CurrentAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                DurationDays = table.Column<int>(type: "INTEGER", nullable: false),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentStreak = table.Column<int>(type: "INTEGER", nullable: false),
                BestStreak = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                Icon = table.Column<string>(type: "TEXT", nullable: false),
                Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                Category = table.Column<int>(type: "INTEGER", nullable: false),
                EstimatedSavingsMin = table.Column<decimal>(type: "TEXT", nullable: true),
                EstimatedSavingsMax = table.Column<decimal>(type: "TEXT", nullable: true),
                IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsChallenges", x => x.SavingsChallengeId);
                table.ForeignKey(
                    name: "FK_SavingsChallenges_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "SavingsGroups",
            columns: table => new
            {
                SavingsGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                GroupType = table.Column<int>(type: "INTEGER", nullable: false),
                PrivacyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                AnonymousMembers = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsGroups", x => x.SavingsGroupId);
                table.ForeignKey(
                    name: "FK_SavingsGroups_AspNetUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedGoals",
            columns: table => new
            {
                SharedGoalId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TargetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedGoals", x => x.SharedGoalId);
                table.ForeignKey(
                    name: "FK_SharedGoals_AspNetUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Subscriptions",
            columns: table => new
            {
                SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                BillingFrequency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                NextBillingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                CancellationUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ManagementUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                AccountEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CancellationDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                CancellationNoticeDays = table.Column<int>(type: "INTEGER", nullable: true),
                LastUsedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                SharedWith = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                AutoDetected = table.Column<bool>(type: "INTEGER", nullable: false),
                DetectedFromTransactionId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                table.ForeignKey(
                    name: "FK_Subscriptions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Subscriptions_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "UserPrivacySettings",
            columns: table => new
            {
                UserPrivacySettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                EnableSocialFeatures = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowGoalSharing = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowSavingsGroups = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowLeaderboards = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowCommunityComparison = table.Column<bool>(type: "INTEGER", nullable: false),
                AnonymousByDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                ShowRealNameInGroups = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                PrivacyTermsAcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPrivacySettings", x => x.UserPrivacySettingsId);
                table.ForeignKey(
                    name: "FK_UserPrivacySettings_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AllowanceTasks",
            columns: table => new
            {
                AllowanceTaskId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ChildAllowanceId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                RewardAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllowanceTasks", x => x.AllowanceTaskId);
                table.ForeignKey(
                    name: "FK_AllowanceTasks_ChildAllowances_ChildAllowanceId",
                    column: x => x.ChildAllowanceId,
                    principalTable: "ChildAllowances",
                    principalColumn: "ChildAllowanceId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BankConnections",
            columns: table => new
            {
                BankConnectionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                BankSourceId = table.Column<int>(type: "INTEGER", nullable: false),
                ApiType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                ExternalAccountId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                AccessToken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                RefreshToken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                TokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                AutoSyncEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BankConnections", x => x.BankConnectionId);
                table.ForeignKey(
                    name: "FK_BankConnections_BankSources_BankSourceId",
                    column: x => x.BankSourceId,
                    principalTable: "BankSources",
                    principalColumn: "BankSourceId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Goals",
            columns: table => new
            {
                GoalId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TargetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                FundedFromBankSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Goals", x => x.GoalId);
                table.ForeignKey(
                    name: "FK_Goals_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Goals_BankSources_FundedFromBankSourceId",
                    column: x => x.FundedFromBankSourceId,
                    principalTable: "BankSources",
                    principalColumn: "BankSourceId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Investments",
            columns: table => new
            {
                InvestmentId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                PurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                BankSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                ShortName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                ISIN = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                Country = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                Market = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                AccountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                SchablonTax = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                SchablonTaxYear = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Investments", x => x.InvestmentId);
                table.ForeignKey(
                    name: "FK_Investments_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Investments_BankSources_BankSourceId",
                    column: x => x.BankSourceId,
                    principalTable: "BankSources",
                    principalColumn: "BankSourceId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Pockets",
            columns: table => new
            {
                PocketId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TargetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                BankSourceId = table.Column<int>(type: "INTEGER", nullable: false),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                MonthlyAllocation = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pockets", x => x.PocketId);
                table.ForeignKey(
                    name: "FK_Pockets_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Pockets_BankSources_BankSourceId",
                    column: x => x.BankSourceId,
                    principalTable: "BankSources",
                    principalColumn: "BankSourceId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BudgetCategories",
            columns: table => new
            {
                BudgetCategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                BudgetId = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                PlannedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                RecurrencePeriodMonths = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BudgetCategories", x => x.BudgetCategoryId);
                table.ForeignKey(
                    name: "FK_BudgetCategories_Budgets_BudgetId",
                    column: x => x.BudgetId,
                    principalTable: "Budgets",
                    principalColumn: "BudgetId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BudgetCategories_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SavingsChallengeProgress",
            columns: table => new
            {
                SavingsChallengeProgressId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SavingsChallengeId = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                Completed = table.Column<bool>(type: "INTEGER", nullable: false),
                AmountSaved = table.Column<decimal>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsChallengeProgress", x => x.SavingsChallengeProgressId);
                table.ForeignKey(
                    name: "FK_SavingsChallengeProgress_SavingsChallenges_SavingsChallengeId",
                    column: x => x.SavingsChallengeId,
                    principalTable: "SavingsChallenges",
                    principalColumn: "SavingsChallengeId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SavingsGroupMembers",
            columns: table => new
            {
                SavingsGroupMemberId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SavingsGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Role = table.Column<int>(type: "INTEGER", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                ShowRealName = table.Column<bool>(type: "INTEGER", nullable: false),
                ShareProgress = table.Column<bool>(type: "INTEGER", nullable: false),
                ShareGoalCount = table.Column<bool>(type: "INTEGER", nullable: false),
                ShareTotalSavings = table.Column<bool>(type: "INTEGER", nullable: false),
                JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsGroupMembers", x => x.SavingsGroupMemberId);
                table.ForeignKey(
                    name: "FK_SavingsGroupMembers_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SavingsGroupMembers_SavingsGroups_SavingsGroupId",
                    column: x => x.SavingsGroupId,
                    principalTable: "SavingsGroups",
                    principalColumn: "SavingsGroupId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedGoalNotifications",
            columns: table => new
            {
                SharedGoalNotificationId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SharedGoalId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedGoalNotifications", x => x.SharedGoalNotificationId);
                table.ForeignKey(
                    name: "FK_SharedGoalNotifications_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SharedGoalNotifications_SharedGoals_SharedGoalId",
                    column: x => x.SharedGoalId,
                    principalTable: "SharedGoals",
                    principalColumn: "SharedGoalId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedGoalParticipants",
            columns: table => new
            {
                SharedGoalParticipantId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SharedGoalId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Role = table.Column<int>(type: "INTEGER", nullable: false),
                JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                InvitationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                InvitedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                InvitedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedGoalParticipants", x => x.SharedGoalParticipantId);
                table.ForeignKey(
                    name: "FK_SharedGoalParticipants_AspNetUsers_InvitedByUserId",
                    column: x => x.InvitedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SharedGoalParticipants_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SharedGoalParticipants_SharedGoals_SharedGoalId",
                    column: x => x.SharedGoalId,
                    principalTable: "SharedGoals",
                    principalColumn: "SharedGoalId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedGoalProposals",
            columns: table => new
            {
                SharedGoalProposalId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SharedGoalId = table.Column<int>(type: "INTEGER", nullable: false),
                ProposedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                ProposalType = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ProposedValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedGoalProposals", x => x.SharedGoalProposalId);
                table.ForeignKey(
                    name: "FK_SharedGoalProposals_AspNetUsers_ProposedByUserId",
                    column: x => x.ProposedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SharedGoalProposals_SharedGoals_SharedGoalId",
                    column: x => x.SharedGoalId,
                    principalTable: "SharedGoals",
                    principalColumn: "SharedGoalId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedGoalTransactions",
            columns: table => new
            {
                SharedGoalTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SharedGoalId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedGoalTransactions", x => x.SharedGoalTransactionId);
                table.ForeignKey(
                    name: "FK_SharedGoalTransactions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SharedGoalTransactions_SharedGoals_SharedGoalId",
                    column: x => x.SharedGoalId,
                    principalTable: "SharedGoals",
                    principalColumn: "SharedGoalId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SubscriptionPriceHistory",
            columns: table => new
            {
                SubscriptionPriceHistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false),
                OldPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                NewPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ChangeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                NotificationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubscriptionPriceHistory", x => x.SubscriptionPriceHistoryId);
                table.ForeignKey(
                    name: "FK_SubscriptionPriceHistory_Subscriptions_SubscriptionId",
                    column: x => x.SubscriptionId,
                    principalTable: "Subscriptions",
                    principalColumn: "SubscriptionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AllowanceTransactions",
            columns: table => new
            {
                AllowanceTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ChildAllowanceId = table.Column<int>(type: "INTEGER", nullable: false),
                AllowanceTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllowanceTransactions", x => x.AllowanceTransactionId);
                table.ForeignKey(
                    name: "FK_AllowanceTransactions_AllowanceTasks_AllowanceTaskId",
                    column: x => x.AllowanceTaskId,
                    principalTable: "AllowanceTasks",
                    principalColumn: "AllowanceTaskId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_AllowanceTransactions_ChildAllowances_ChildAllowanceId",
                    column: x => x.ChildAllowanceId,
                    principalTable: "ChildAllowances",
                    principalColumn: "ChildAllowanceId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GoalMilestones",
            columns: table => new
            {
                GoalMilestoneId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GoalId = table.Column<int>(type: "INTEGER", nullable: false),
                TargetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Percentage = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                IsReached = table.Column<bool>(type: "INTEGER", nullable: false),
                ReachedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                IsAutomatic = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoalMilestones", x => x.GoalMilestoneId);
                table.ForeignKey(
                    name: "FK_GoalMilestones_Goals_GoalId",
                    column: x => x.GoalId,
                    principalTable: "Goals",
                    principalColumn: "GoalId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GoalShares",
            columns: table => new
            {
                GoalShareId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GoalId = table.Column<int>(type: "INTEGER", nullable: true),
                SharedGoalId = table.Column<int>(type: "INTEGER", nullable: true),
                ShareToken = table.Column<string>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                ShowCurrentAmount = table.Column<bool>(type: "INTEGER", nullable: false),
                ShowTargetAmount = table.Column<bool>(type: "INTEGER", nullable: false),
                ShowTargetDate = table.Column<bool>(type: "INTEGER", nullable: false),
                ShowTransactions = table.Column<bool>(type: "INTEGER", nullable: false),
                ShowOwnerName = table.Column<bool>(type: "INTEGER", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                ViewCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoalShares", x => x.GoalShareId);
                table.ForeignKey(
                    name: "FK_GoalShares_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GoalShares_Goals_GoalId",
                    column: x => x.GoalId,
                    principalTable: "Goals",
                    principalColumn: "GoalId");
                table.ForeignKey(
                    name: "FK_GoalShares_SharedGoals_SharedGoalId",
                    column: x => x.SharedGoalId,
                    principalTable: "SharedGoals",
                    principalColumn: "SharedGoalId");
            });

        migrationBuilder.CreateTable(
            name: "GroupGoals",
            columns: table => new
            {
                GroupGoalId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SavingsGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                GoalId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                IsAnonymous = table.Column<bool>(type: "INTEGER", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                SharedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupGoals", x => x.GroupGoalId);
                table.ForeignKey(
                    name: "FK_GroupGoals_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GroupGoals_Goals_GoalId",
                    column: x => x.GoalId,
                    principalTable: "Goals",
                    principalColumn: "GoalId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GroupGoals_SavingsGroups_SavingsGroupId",
                    column: x => x.SavingsGroupId,
                    principalTable: "SavingsGroups",
                    principalColumn: "SavingsGroupId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoundUpSettings",
            columns: table => new
            {
                RoundUpSettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                RoundUpAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                TargetGoalId = table.Column<int>(type: "INTEGER", nullable: true),
                EnableEmployerMatching = table.Column<bool>(type: "INTEGER", nullable: false),
                EmployerMatchingPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                EmployerMatchingMonthlyLimit = table.Column<decimal>(type: "TEXT", nullable: true),
                EnableSalaryAutoSave = table.Column<bool>(type: "INTEGER", nullable: false),
                SalaryAutoSavePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                MinimumTransactionAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                MaximumTransactionAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                OnlyExpenses = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoundUpSettings", x => x.RoundUpSettingsId);
                table.ForeignKey(
                    name: "FK_RoundUpSettings_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_RoundUpSettings_Goals_TargetGoalId",
                    column: x => x.TargetGoalId,
                    principalTable: "Goals",
                    principalColumn: "GoalId");
            });

        migrationBuilder.CreateTable(
            name: "CapitalGains",
            columns: table => new
            {
                CapitalGainId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvestmentId = table.Column<int>(type: "INTEGER", nullable: false),
                SaleDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                PurchasePricePerUnit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TotalPurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                SalePricePerUnit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TotalSalePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Gain = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                SecurityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                SecurityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ISIN = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                TaxYear = table.Column<int>(type: "INTEGER", nullable: false),
                IsISK = table.Column<bool>(type: "INTEGER", nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CapitalGains", x => x.CapitalGainId);
                table.ForeignKey(
                    name: "FK_CapitalGains_Investments_InvestmentId",
                    column: x => x.InvestmentId,
                    principalTable: "Investments",
                    principalColumn: "InvestmentId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Dividends",
            columns: table => new
            {
                DividendId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvestmentId = table.Column<int>(type: "INTEGER", nullable: false),
                PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                ExDividendDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                AmountPerShare = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                SharesHeld = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                TaxWithheld = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                IsReinvested = table.Column<bool>(type: "INTEGER", nullable: false),
                ReinvestedShares = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                ReinvestmentPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Dividends", x => x.DividendId);
                table.ForeignKey(
                    name: "FK_Dividends_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Dividends_Investments_InvestmentId",
                    column: x => x.InvestmentId,
                    principalTable: "Investments",
                    principalColumn: "InvestmentId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InvestmentTransactions",
            columns: table => new
            {
                InvestmentTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvestmentId = table.Column<int>(type: "INTEGER", nullable: false),
                TransactionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                PricePerShare = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Fees = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvestmentTransactions", x => x.InvestmentTransactionId);
                table.ForeignKey(
                    name: "FK_InvestmentTransactions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InvestmentTransactions_Investments_InvestmentId",
                    column: x => x.InvestmentId,
                    principalTable: "Investments",
                    principalColumn: "InvestmentId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PocketTransactions",
            columns: table => new
            {
                PocketTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PocketId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                RelatedPocketId = table.Column<int>(type: "INTEGER", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PocketTransactions", x => x.PocketTransactionId);
                table.ForeignKey(
                    name: "FK_PocketTransactions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PocketTransactions_Pockets_PocketId",
                    column: x => x.PocketId,
                    principalTable: "Pockets",
                    principalColumn: "PocketId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PocketTransactions_Pockets_RelatedPocketId",
                    column: x => x.RelatedPocketId,
                    principalTable: "Pockets",
                    principalColumn: "PocketId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                TransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsIncome = table.Column<bool>(type: "INTEGER", nullable: false),
                BankSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                Payee = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                RecurringId = table.Column<int>(type: "INTEGER", nullable: true),
                Imported = table.Column<bool>(type: "INTEGER", nullable: false),
                ImportSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Cleared = table.Column<bool>(type: "INTEGER", nullable: false),
                IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: true),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                RecipientBankgiro = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                RecipientPlusgiro = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                OCR = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                PocketId = table.Column<int>(type: "INTEGER", nullable: true),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                table.ForeignKey(
                    name: "FK_Transactions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Transactions_BankSources_BankSourceId",
                    column: x => x.BankSourceId,
                    principalTable: "BankSources",
                    principalColumn: "BankSourceId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Transactions_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "HouseholdId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Transactions_Pockets_PocketId",
                    column: x => x.PocketId,
                    principalTable: "Pockets",
                    principalColumn: "PocketId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "BudgetAlerts",
            columns: table => new
            {
                BudgetAlertId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                BudgetId = table.Column<int>(type: "INTEGER", nullable: false),
                BudgetCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                ThresholdPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                CurrentPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                SpentAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                PlannedAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                TriggeredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ForecastDaysUntilExceeded = table.Column<int>(type: "INTEGER", nullable: true),
                DailyRate = table.Column<decimal>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BudgetAlerts", x => x.BudgetAlertId);
                table.ForeignKey(
                    name: "FK_BudgetAlerts_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_BudgetAlerts_BudgetCategories_BudgetCategoryId",
                    column: x => x.BudgetCategoryId,
                    principalTable: "BudgetCategories",
                    principalColumn: "BudgetCategoryId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BudgetAlerts_Budgets_BudgetId",
                    column: x => x.BudgetId,
                    principalTable: "Budgets",
                    principalColumn: "BudgetId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BudgetFreezes",
            columns: table => new
            {
                BudgetFreezeId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                BudgetId = table.Column<int>(type: "INTEGER", nullable: false),
                BudgetCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                FrozenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UnfrozenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                Reason = table.Column<string>(type: "TEXT", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BudgetFreezes", x => x.BudgetFreezeId);
                table.ForeignKey(
                    name: "FK_BudgetFreezes_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_BudgetFreezes_BudgetCategories_BudgetCategoryId",
                    column: x => x.BudgetCategoryId,
                    principalTable: "BudgetCategories",
                    principalColumn: "BudgetCategoryId");
                table.ForeignKey(
                    name: "FK_BudgetFreezes_Budgets_BudgetId",
                    column: x => x.BudgetId,
                    principalTable: "Budgets",
                    principalColumn: "BudgetId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedGoalProposalVotes",
            columns: table => new
            {
                SharedGoalProposalVoteId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SharedGoalProposalId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Vote = table.Column<int>(type: "INTEGER", nullable: false),
                VotedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedGoalProposalVotes", x => x.SharedGoalProposalVoteId);
                table.ForeignKey(
                    name: "FK_SharedGoalProposalVotes_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SharedGoalProposalVotes_SharedGoalProposals_SharedGoalProposalId",
                    column: x => x.SharedGoalProposalId,
                    principalTable: "SharedGoalProposals",
                    principalColumn: "SharedGoalProposalId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GroupComments",
            columns: table => new
            {
                GroupCommentId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SavingsGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                GroupGoalId = table.Column<int>(type: "INTEGER", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupComments", x => x.GroupCommentId);
                table.ForeignKey(
                    name: "FK_GroupComments_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GroupComments_GroupGoals_GroupGoalId",
                    column: x => x.GroupGoalId,
                    principalTable: "GroupGoals",
                    principalColumn: "GroupGoalId");
                table.ForeignKey(
                    name: "FK_GroupComments_SavingsGroups_SavingsGroupId",
                    column: x => x.SavingsGroupId,
                    principalTable: "SavingsGroups",
                    principalColumn: "SavingsGroupId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Bills",
            columns: table => new
            {
                BillId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                IssueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                RecurringFrequency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                OCR = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Bankgiro = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Plusgiro = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Payee = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                HouseholdId = table.Column<int>(type: "INTEGER", nullable: true),
                TransactionId = table.Column<int>(type: "INTEGER", nullable: true),
                DocumentPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bills", x => x.BillId);
                table.ForeignKey(
                    name: "FK_Bills_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Bills_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Bills_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "HouseholdId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Bills_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "TransactionId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Receipts",
            columns: table => new
            {
                ReceiptId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Merchant = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ReceiptDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                ReceiptType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                OcrText = table.Column<string>(type: "TEXT", nullable: true),
                ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TransactionId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Receipts", x => x.ReceiptId);
                table.ForeignKey(
                    name: "FK_Receipts_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Receipts_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "TransactionId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "RoundUpTransactions",
            columns: table => new
            {
                RoundUpTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                OriginalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                RoundedAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                RoundUpAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                EmployerMatchingAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                TotalSaved = table.Column<decimal>(type: "TEXT", nullable: false),
                GoalId = table.Column<int>(type: "INTEGER", nullable: true),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                IsAutomatic = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoundUpTransactions", x => x.RoundUpTransactionId);
                table.ForeignKey(
                    name: "FK_RoundUpTransactions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_RoundUpTransactions_Goals_GoalId",
                    column: x => x.GoalId,
                    principalTable: "Goals",
                    principalColumn: "GoalId");
                table.ForeignKey(
                    name: "FK_RoundUpTransactions_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "TransactionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TaxDeductions",
            columns: table => new
            {
                TaxDeductionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                DeductibleAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ServiceProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                OrganizationNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                TaxYear = table.Column<int>(type: "INTEGER", nullable: false),
                Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                WorkDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                WorkDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaxDeductions", x => x.TaxDeductionId);
                table.ForeignKey(
                    name: "FK_TaxDeductions_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "TransactionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TransactionCategories",
            columns: table => new
            {
                TransactionCategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Percentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionCategories", x => x.TransactionCategoryId);
                table.ForeignKey(
                    name: "FK_TransactionCategories_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TransactionCategories_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "TransactionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserFeedbacks",
            columns: table => new
            {
                FeedbackId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                TransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                PredictedCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                PredictedConfidence = table.Column<float>(type: "REAL", nullable: false),
                ActualCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                WasCorrectionNeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                FeedbackDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFeedbacks", x => x.FeedbackId);
                table.ForeignKey(
                    name: "FK_UserFeedbacks_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserFeedbacks_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "TransactionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CommentLikes",
            columns: table => new
            {
                CommentLikeId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GroupCommentId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommentLikes", x => x.CommentLikeId);
                table.ForeignKey(
                    name: "FK_CommentLikes_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CommentLikes_GroupComments_GroupCommentId",
                    column: x => x.GroupCommentId,
                    principalTable: "GroupComments",
                    principalColumn: "GroupCommentId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BillReminders",
            columns: table => new
            {
                BillReminderId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                BillId = table.Column<int>(type: "INTEGER", nullable: false),
                ReminderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsSent = table.Column<bool>(type: "INTEGER", nullable: false),
                SentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ReminderMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                SnoozeUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                SnoozeCount = table.Column<int>(type: "INTEGER", nullable: false),
                IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                EscalationLevel = table.Column<int>(type: "INTEGER", nullable: false),
                LastFollowUpDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BillReminders", x => x.BillReminderId);
                table.ForeignKey(
                    name: "FK_BillReminders_Bills_BillId",
                    column: x => x.BillId,
                    principalTable: "Bills",
                    principalColumn: "BillId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReceiptLineItems",
            columns: table => new
            {
                ReceiptLineItemId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ReceiptId = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TotalPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TaxRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReceiptLineItems", x => x.ReceiptLineItemId);
                table.ForeignKey(
                    name: "FK_ReceiptLineItems_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "CategoryId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ReceiptLineItems_Receipts_ReceiptId",
                    column: x => x.ReceiptId,
                    principalTable: "Receipts",
                    principalColumn: "ReceiptId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "BankSources",
            columns: new[] { "BankSourceId", "AccountType", "ClosedDate", "Color", "CreatedAt", "Currency", "InitialBalance", "Institution", "Logo", "Name", "OpenedDate", "UpdatedAt", "UserId", "ValidFrom", "ValidTo" },
            values: new object[,]
            {
                { 1, "checking", null, "#DC143C", new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7033), "SEK", 0m, null, null, "ICA-banken", null, null, null, new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7225), null },
                { 2, "checking", null, "#FF8C00", new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7635), "SEK", 0m, null, null, "Swedbank", null, null, null, new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7636), null },
                { 3, "checking", null, "#0066CC", new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7638), "SEK", 0m, null, null, "SEB", null, null, null, new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7639), null },
                { 4, "checking", null, "#00A9CE", new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7641), "SEK", 0m, null, null, "Nordea", null, null, null, new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7641), null },
                { 5, "checking", null, "#003366", new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7644), "SEK", 0m, null, null, "Handelsbanken", null, null, null, new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7644), null },
                { 6, "investment", null, "#006400", new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7646), "SEK", 0m, null, null, "Avanza", null, null, null, new DateTime(2025, 11, 6, 6, 14, 25, 687, DateTimeKind.Utc).AddTicks(7647), null }
            });

        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "CategoryId", "AccountNumber", "Color", "CreatedAt", "DefaultBudgetMonthly", "IsSystemCategory", "Name", "OriginalAccountNumber", "OriginalColor", "OriginalName", "ParentId", "TaxRelated", "UpdatedAt" },
            values: new object[,]
            {
                { 1, "5000", "#FF6B6B", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8456), null, true, "Mat & Dryck", "5000", "#FF6B6B", "Mat & Dryck", null, false, null },
                { 2, "6000", "#4ECDC4", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8660), null, true, "Transport", "6000", "#4ECDC4", "Transport", null, false, null },
                { 3, "4000", "#45B7D1", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8663), null, true, "Boende", "4000", "#45B7D1", "Boende", null, false, null },
                { 4, "7000", "#FFA07A", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8666), null, true, "Nöje", "7000", "#FFA07A", "Nöje", null, false, null },
                { 5, "5500", "#98D8C8", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8669), null, true, "Shopping", "5500", "#98D8C8", "Shopping", null, false, null },
                { 6, "7500", "#6BCF7F", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8672), null, true, "Hälsa", "7500", "#6BCF7F", "Hälsa", null, false, null },
                { 7, "3000", "#4CAF50", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8674), null, true, "Lön", "3000", "#4CAF50", "Lön", null, false, null },
                { 8, "8000", "#2196F3", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8676), null, true, "Sparande", "8000", "#2196F3", "Sparande", null, false, null },
                { 9, "6900", "#9E9E9E", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(8678), null, true, "Övrigt", "6900", "#9E9E9E", "Övrigt", null, false, null },
                { 10, "5100", "#FF6B6B", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9004), null, true, "Livsmedel", "5100", "#FF6B6B", "Livsmedel", 1, false, null },
                { 11, "5200", "#FF5252", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9011), null, true, "Restaurang", "5200", "#FF5252", "Restaurang", 1, false, null },
                { 12, "5300", "#FF8A80", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9014), null, true, "Café", "5300", "#FF8A80", "Café", 1, false, null },
                { 13, "6100", "#4ECDC4", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9017), null, true, "Kollektivtrafik", "6100", "#4ECDC4", "Kollektivtrafik", 2, false, null },
                { 14, "6200", "#26A69A", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9020), null, true, "Bensin", "6200", "#26A69A", "Bensin", 2, false, null },
                { 15, "6500", "#80CBC4", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9022), null, true, "Parkering", "6500", "#80CBC4", "Parkering", 2, false, null },
                { 16, "4100", "#45B7D1", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9024), null, true, "Hyra/Avgift", "4100", "#45B7D1", "Hyra/Avgift", 3, false, null },
                { 17, "4200", "#29B6F6", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9027), null, true, "El", "4200", "#29B6F6", "El", 3, false, null },
                { 18, "4300", "#81D4FA", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9044), null, true, "Bredband", "4300", "#81D4FA", "Bredband", 3, false, null },
                { 19, "4400", "#4FC3F7", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9047), null, true, "Hemförsäkring", "4400", "#4FC3F7", "Hemförsäkring", 3, false, null },
                { 20, "7100", "#FFA07A", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9049), null, true, "Streaming", "7100", "#FFA07A", "Streaming", 4, false, null },
                { 21, "7300", "#FF8A65", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9051), null, true, "Gym", "7300", "#FF8A65", "Gym", 4, false, null },
                { 22, "7400", "#FFAB91", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9054), null, true, "Resor", "7400", "#FFAB91", "Resor", 4, false, null },
                { 23, "5510", "#98D8C8", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9056), null, true, "Kläder", "5510", "#98D8C8", "Kläder", 5, false, null },
                { 24, "5520", "#80CBC4", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9058), null, true, "Hygienartiklar", "5520", "#80CBC4", "Hygienartiklar", 5, false, null },
                { 25, "5550", "#B2DFDB", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9060), null, true, "Elektronik", "5550", "#B2DFDB", "Elektronik", 5, false, null },
                { 26, "7510", "#6BCF7F", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9063), null, true, "Tandvård", "7510", "#6BCF7F", "Tandvård", 6, false, null },
                { 27, "7520", "#81C784", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9065), null, true, "Läkarvård", "7520", "#81C784", "Läkarvård", 6, false, null },
                { 28, "7530", "#A5D6A7", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9067), null, true, "Medicin", "7530", "#A5D6A7", "Medicin", 6, false, null },
                { 29, "3010", "#66BB6A", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9070), null, true, "Bonus", "3010", "#66BB6A", "Bonus", 7, false, null },
                { 30, "3020", "#81C784", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9072), null, true, "Semesterersättning", "3020", "#81C784", "Semesterersättning", 7, false, null },
                { 31, "8100", "#2196F3", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9074), null, true, "Buffert", "8100", "#2196F3", "Buffert", 8, false, null },
                { 32, "8200", "#42A5F5", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9076), null, true, "Månadsspar Fonder", "8200", "#42A5F5", "Månadsspar Fonder", 8, false, null },
                { 33, "8300", "#64B5F6", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9079), null, true, "ISK", "8300", "#64B5F6", "ISK", 8, false, null },
                { 34, "8400", "#90CAF9", new DateTime(2025, 11, 6, 6, 14, 25, 686, DateTimeKind.Utc).AddTicks(9081), null, true, "Pensionssparande", "8400", "#90CAF9", "Pensionssparande", 8, false, null }
            });

        migrationBuilder.CreateIndex(
            name: "IX_AllowanceTasks_ChildAllowanceId",
            table: "AllowanceTasks",
            column: "ChildAllowanceId");

        migrationBuilder.CreateIndex(
            name: "IX_AllowanceTransactions_AllowanceTaskId",
            table: "AllowanceTransactions",
            column: "AllowanceTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_AllowanceTransactions_ChildAllowanceId",
            table: "AllowanceTransactions",
            column: "ChildAllowanceId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoleClaims_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_RoleId",
            table: "AspNetUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_HouseholdMemberId",
            table: "AspNetUsers",
            column: "HouseholdMemberId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Assets_UserId",
            table: "Assets",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Assets_ValidFrom",
            table: "Assets",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Assets_ValidFrom_ValidTo",
            table: "Assets",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Assets_ValidTo",
            table: "Assets",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_Action",
            table: "AuditLogEntries",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_Category",
            table: "AuditLogEntries",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_HouseholdId",
            table: "AuditLogEntries",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_HouseholdId_Timestamp",
            table: "AuditLogEntries",
            columns: new[] { "HouseholdId", "Timestamp" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_Severity",
            table: "AuditLogEntries",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_Timestamp",
            table: "AuditLogEntries",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_UserId",
            table: "AuditLogEntries",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_UserId_Timestamp",
            table: "AuditLogEntries",
            columns: new[] { "UserId", "Timestamp" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Action",
            table: "AuditLogs",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CreatedAt",
            table: "AuditLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EntityType",
            table: "AuditLogs",
            column: "EntityType");

        migrationBuilder.CreateIndex(
            name: "IX_BankConnections_BankSourceId",
            table: "BankConnections",
            column: "BankSourceId");

        migrationBuilder.CreateIndex(
            name: "IX_BankConnections_ExternalAccountId",
            table: "BankConnections",
            column: "ExternalAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_BankSources_UserId",
            table: "BankSources",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_BankSources_ValidFrom",
            table: "BankSources",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_BankSources_ValidFrom_ValidTo",
            table: "BankSources",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_BankSources_ValidTo",
            table: "BankSources",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_BillReminders_BillId",
            table: "BillReminders",
            column: "BillId");

        migrationBuilder.CreateIndex(
            name: "IX_BillReminders_IsSent",
            table: "BillReminders",
            column: "IsSent");

        migrationBuilder.CreateIndex(
            name: "IX_BillReminders_ReminderDate",
            table: "BillReminders",
            column: "ReminderDate");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_CategoryId",
            table: "Bills",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_DueDate",
            table: "Bills",
            column: "DueDate");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_HouseholdId",
            table: "Bills",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_IsRecurring",
            table: "Bills",
            column: "IsRecurring");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_Status",
            table: "Bills",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_TransactionId",
            table: "Bills",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_Bills_UserId",
            table: "Bills",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetAlerts_BudgetCategoryId",
            table: "BudgetAlerts",
            column: "BudgetCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetAlerts_BudgetId",
            table: "BudgetAlerts",
            column: "BudgetId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetAlerts_UserId",
            table: "BudgetAlerts",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetAlertSettings_UserId",
            table: "BudgetAlertSettings",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetCategories_BudgetId",
            table: "BudgetCategories",
            column: "BudgetId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetCategories_CategoryId",
            table: "BudgetCategories",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetFreezes_BudgetCategoryId",
            table: "BudgetFreezes",
            column: "BudgetCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetFreezes_BudgetId",
            table: "BudgetFreezes",
            column: "BudgetId");

        migrationBuilder.CreateIndex(
            name: "IX_BudgetFreezes_UserId",
            table: "BudgetFreezes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_HouseholdId",
            table: "Budgets",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_UserId",
            table: "Budgets",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_ValidFrom",
            table: "Budgets",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_ValidFrom_ValidTo",
            table: "Budgets",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_ValidTo",
            table: "Budgets",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_CapitalGains_InvestmentId",
            table: "CapitalGains",
            column: "InvestmentId");

        migrationBuilder.CreateIndex(
            name: "IX_CapitalGains_IsISK",
            table: "CapitalGains",
            column: "IsISK");

        migrationBuilder.CreateIndex(
            name: "IX_CapitalGains_SaleDate",
            table: "CapitalGains",
            column: "SaleDate");

        migrationBuilder.CreateIndex(
            name: "IX_CapitalGains_TaxYear",
            table: "CapitalGains",
            column: "TaxYear");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_ParentId",
            table: "Categories",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_CategoryId",
            table: "CategoryRules",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_IsActive",
            table: "CategoryRules",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_IsActive_Priority",
            table: "CategoryRules",
            columns: new[] { "IsActive", "Priority" });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_OverridesSystemRuleId",
            table: "CategoryRules",
            column: "OverridesSystemRuleId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_Priority",
            table: "CategoryRules",
            column: "Priority");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_RuleType",
            table: "CategoryRules",
            column: "RuleType");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_RuleType_UserId",
            table: "CategoryRules",
            columns: new[] { "RuleType", "UserId" });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryRules_UserId",
            table: "CategoryRules",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChildAllowances_HouseholdMemberId",
            table: "ChildAllowances",
            column: "HouseholdMemberId");

        migrationBuilder.CreateIndex(
            name: "IX_CommentLikes_GroupCommentId",
            table: "CommentLikes",
            column: "GroupCommentId");

        migrationBuilder.CreateIndex(
            name: "IX_CommentLikes_UserId",
            table: "CommentLikes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_CommuteDeductions_Date",
            table: "CommuteDeductions",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_CommuteDeductions_TaxYear",
            table: "CommuteDeductions",
            column: "TaxYear");

        migrationBuilder.CreateIndex(
            name: "IX_CreditRatings_CheckedDate",
            table: "CreditRatings",
            column: "CheckedDate");

        migrationBuilder.CreateIndex(
            name: "IX_CreditRatings_HouseholdId",
            table: "CreditRatings",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_CurrencyAccounts_Currency",
            table: "CurrencyAccounts",
            column: "Currency");

        migrationBuilder.CreateIndex(
            name: "IX_CurrencyAccounts_UserId",
            table: "CurrencyAccounts",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Dividends_InvestmentId",
            table: "Dividends",
            column: "InvestmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Dividends_PaymentDate",
            table: "Dividends",
            column: "PaymentDate");

        migrationBuilder.CreateIndex(
            name: "IX_Dividends_UserId",
            table: "Dividends",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_DoNotDisturbSchedules_UserId",
            table: "DoNotDisturbSchedules",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseShares_HouseholdMemberId",
            table: "ExpenseShares",
            column: "HouseholdMemberId");

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseShares_SharedExpenseId",
            table: "ExpenseShares",
            column: "SharedExpenseId");

        migrationBuilder.CreateIndex(
            name: "IX_GoalMilestones_GoalId",
            table: "GoalMilestones",
            column: "GoalId");

        migrationBuilder.CreateIndex(
            name: "IX_GoalMilestones_GoalId_Percentage",
            table: "GoalMilestones",
            columns: new[] { "GoalId", "Percentage" });

        migrationBuilder.CreateIndex(
            name: "IX_GoalMilestones_IsReached",
            table: "GoalMilestones",
            column: "IsReached");

        migrationBuilder.CreateIndex(
            name: "IX_Goals_FundedFromBankSourceId",
            table: "Goals",
            column: "FundedFromBankSourceId");

        migrationBuilder.CreateIndex(
            name: "IX_Goals_UserId",
            table: "Goals",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Goals_ValidFrom",
            table: "Goals",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Goals_ValidFrom_ValidTo",
            table: "Goals",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Goals_ValidTo",
            table: "Goals",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_GoalShares_GoalId",
            table: "GoalShares",
            column: "GoalId");

        migrationBuilder.CreateIndex(
            name: "IX_GoalShares_SharedGoalId",
            table: "GoalShares",
            column: "SharedGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_GoalShares_UserId",
            table: "GoalShares",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupComments_GroupGoalId",
            table: "GroupComments",
            column: "GroupGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupComments_SavingsGroupId",
            table: "GroupComments",
            column: "SavingsGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupComments_UserId",
            table: "GroupComments",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupGoals_GoalId",
            table: "GroupGoals",
            column: "GoalId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupGoals_SavingsGroupId",
            table: "GroupGoals",
            column: "SavingsGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupGoals_UserId",
            table: "GroupGoals",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdMembers_HouseholdId",
            table: "HouseholdMembers",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdRoles_DelegationEndDate",
            table: "HouseholdRoles",
            column: "DelegationEndDate");

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdRoles_HouseholdMemberId",
            table: "HouseholdRoles",
            column: "HouseholdMemberId");

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdRoles_HouseholdMemberId_IsActive",
            table: "HouseholdRoles",
            columns: new[] { "HouseholdMemberId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdRoles_IsActive",
            table: "HouseholdRoles",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_HouseholdRoles_RoleType",
            table: "HouseholdRoles",
            column: "RoleType");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_AccountNumber",
            table: "Investments",
            column: "AccountNumber");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_AccountType",
            table: "Investments",
            column: "AccountType");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_BankSourceId",
            table: "Investments",
            column: "BankSourceId");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_ISIN",
            table: "Investments",
            column: "ISIN");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_SchablonTaxYear",
            table: "Investments",
            column: "SchablonTaxYear");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_UserId",
            table: "Investments",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_ValidFrom",
            table: "Investments",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Investments_ValidFrom_ValidTo",
            table: "Investments",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Investments_ValidTo",
            table: "Investments",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_InvestmentTransactions_InvestmentId",
            table: "InvestmentTransactions",
            column: "InvestmentId");

        migrationBuilder.CreateIndex(
            name: "IX_InvestmentTransactions_TransactionDate",
            table: "InvestmentTransactions",
            column: "TransactionDate");

        migrationBuilder.CreateIndex(
            name: "IX_InvestmentTransactions_UserId",
            table: "InvestmentTransactions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineMilestones_MilestoneType",
            table: "LifeTimelineMilestones",
            column: "MilestoneType");

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineMilestones_PlannedDate",
            table: "LifeTimelineMilestones",
            column: "PlannedDate");

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineMilestones_UserId",
            table: "LifeTimelineMilestones",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineMilestones_UserId_PlannedDate",
            table: "LifeTimelineMilestones",
            columns: new[] { "UserId", "PlannedDate" });

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineScenarios_IsActive",
            table: "LifeTimelineScenarios",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineScenarios_UserId",
            table: "LifeTimelineScenarios",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_LifeTimelineScenarios_UserId_IsActive",
            table: "LifeTimelineScenarios",
            columns: new[] { "UserId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_Loans_Type",
            table: "Loans",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Loans_UserId",
            table: "Loans",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Loans_ValidFrom",
            table: "Loans",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Loans_ValidFrom_ValidTo",
            table: "Loans",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Loans_ValidTo",
            table: "Loans",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_MLModels_UserId",
            table: "MLModels",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_NetWorthSnapshots_Date",
            table: "NetWorthSnapshots",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_NetWorthSnapshots_UserId",
            table: "NetWorthSnapshots",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_NetWorthSnapshots_UserId_Date",
            table: "NetWorthSnapshots",
            columns: new[] { "UserId", "Date" });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationIntegrations_UserId",
            table: "NotificationIntegrations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationPreferences_UserId",
            table: "NotificationPreferences",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId",
            table: "Notifications",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Pensions_PensionType",
            table: "Pensions",
            column: "PensionType");

        migrationBuilder.CreateIndex(
            name: "IX_Pensions_UserId",
            table: "Pensions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Pockets_BankSourceId",
            table: "Pockets",
            column: "BankSourceId");

        migrationBuilder.CreateIndex(
            name: "IX_Pockets_UserId",
            table: "Pockets",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Pockets_ValidFrom",
            table: "Pockets",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Pockets_ValidFrom_ValidTo",
            table: "Pockets",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Pockets_ValidTo",
            table: "Pockets",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_PocketTransactions_PocketId",
            table: "PocketTransactions",
            column: "PocketId");

        migrationBuilder.CreateIndex(
            name: "IX_PocketTransactions_RelatedPocketId",
            table: "PocketTransactions",
            column: "RelatedPocketId");

        migrationBuilder.CreateIndex(
            name: "IX_PocketTransactions_TransactionDate",
            table: "PocketTransactions",
            column: "TransactionDate");

        migrationBuilder.CreateIndex(
            name: "IX_PocketTransactions_UserId",
            table: "PocketTransactions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_PortfolioAllocations_IsActive",
            table: "PortfolioAllocations",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_PortfolioAllocations_UserId",
            table: "PortfolioAllocations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ReceiptLineItems_CategoryId",
            table: "ReceiptLineItems",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_ReceiptLineItems_ReceiptId",
            table: "ReceiptLineItems",
            column: "ReceiptId");

        migrationBuilder.CreateIndex(
            name: "IX_Receipts_Merchant",
            table: "Receipts",
            column: "Merchant");

        migrationBuilder.CreateIndex(
            name: "IX_Receipts_ReceiptDate",
            table: "Receipts",
            column: "ReceiptDate");

        migrationBuilder.CreateIndex(
            name: "IX_Receipts_TransactionId",
            table: "Receipts",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_Receipts_UserId",
            table: "Receipts",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Reminders_UserId",
            table: "Reminders",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ReminderSettings_UserId",
            table: "ReminderSettings",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionKey",
            table: "RolePermissions",
            column: "PermissionKey");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_RoleType",
            table: "RolePermissions",
            column: "RoleType");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_RoleType_PermissionKey",
            table: "RolePermissions",
            columns: new[] { "RoleType", "PermissionKey" });

        migrationBuilder.CreateIndex(
            name: "IX_RoundUpSettings_TargetGoalId",
            table: "RoundUpSettings",
            column: "TargetGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_RoundUpSettings_UserId",
            table: "RoundUpSettings",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RoundUpTransactions_GoalId",
            table: "RoundUpTransactions",
            column: "GoalId");

        migrationBuilder.CreateIndex(
            name: "IX_RoundUpTransactions_TransactionId",
            table: "RoundUpTransactions",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_RoundUpTransactions_UserId",
            table: "RoundUpTransactions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryHistories_IsCurrent",
            table: "SalaryHistories",
            column: "IsCurrent");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryHistories_Period",
            table: "SalaryHistories",
            column: "Period");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryHistories_UserId",
            table: "SalaryHistories",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryHistories_UserId_Period",
            table: "SalaryHistories",
            columns: new[] { "UserId", "Period" });

        migrationBuilder.CreateIndex(
            name: "IX_SavingsChallengeProgress_SavingsChallengeId",
            table: "SavingsChallengeProgress",
            column: "SavingsChallengeId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsChallenges_UserId",
            table: "SavingsChallenges",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsGroupMembers_SavingsGroupId",
            table: "SavingsGroupMembers",
            column: "SavingsGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsGroupMembers_UserId",
            table: "SavingsGroupMembers",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsGroups_CreatedByUserId",
            table: "SavingsGroups",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedExpenses_HouseholdId",
            table: "SharedExpenses",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalNotifications_IsRead",
            table: "SharedGoalNotifications",
            column: "IsRead");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalNotifications_SharedGoalId",
            table: "SharedGoalNotifications",
            column: "SharedGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalNotifications_UserId",
            table: "SharedGoalNotifications",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalNotifications_UserId_IsRead",
            table: "SharedGoalNotifications",
            columns: new[] { "UserId", "IsRead" });

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalParticipants_InvitationStatus",
            table: "SharedGoalParticipants",
            column: "InvitationStatus");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalParticipants_InvitedByUserId",
            table: "SharedGoalParticipants",
            column: "InvitedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalParticipants_SharedGoalId",
            table: "SharedGoalParticipants",
            column: "SharedGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalParticipants_SharedGoalId_UserId",
            table: "SharedGoalParticipants",
            columns: new[] { "SharedGoalId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalParticipants_UserId",
            table: "SharedGoalParticipants",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalProposals_ProposedByUserId",
            table: "SharedGoalProposals",
            column: "ProposedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalProposals_SharedGoalId",
            table: "SharedGoalProposals",
            column: "SharedGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalProposals_Status",
            table: "SharedGoalProposals",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalProposalVotes_SharedGoalProposalId",
            table: "SharedGoalProposalVotes",
            column: "SharedGoalProposalId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalProposalVotes_SharedGoalProposalId_UserId",
            table: "SharedGoalProposalVotes",
            columns: new[] { "SharedGoalProposalId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalProposalVotes_UserId",
            table: "SharedGoalProposalVotes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoals_CreatedByUserId",
            table: "SharedGoals",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoals_Status",
            table: "SharedGoals",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalTransactions_SharedGoalId",
            table: "SharedGoalTransactions",
            column: "SharedGoalId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalTransactions_TransactionDate",
            table: "SharedGoalTransactions",
            column: "TransactionDate");

        migrationBuilder.CreateIndex(
            name: "IX_SharedGoalTransactions_UserId",
            table: "SharedGoalTransactions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SubscriptionPriceHistory_ChangeDate",
            table: "SubscriptionPriceHistory",
            column: "ChangeDate");

        migrationBuilder.CreateIndex(
            name: "IX_SubscriptionPriceHistory_SubscriptionId",
            table: "SubscriptionPriceHistory",
            column: "SubscriptionId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_CategoryId",
            table: "Subscriptions",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_IsActive",
            table: "Subscriptions",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_NextBillingDate",
            table: "Subscriptions",
            column: "NextBillingDate");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_UserId",
            table: "Subscriptions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_TaxDeductions_TaxYear",
            table: "TaxDeductions",
            column: "TaxYear");

        migrationBuilder.CreateIndex(
            name: "IX_TaxDeductions_TransactionId",
            table: "TaxDeductions",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_TaxDeductions_Type",
            table: "TaxDeductions",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionCategories_CategoryId",
            table: "TransactionCategories",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionCategories_TransactionId",
            table: "TransactionCategories",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_BankSourceId_Date",
            table: "Transactions",
            columns: new[] { "BankSourceId", "Date" });

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_Date",
            table: "Transactions",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_HouseholdId",
            table: "Transactions",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_HouseholdId_Date",
            table: "Transactions",
            columns: new[] { "HouseholdId", "Date" });

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_IsRecurring",
            table: "Transactions",
            column: "IsRecurring");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_Payee",
            table: "Transactions",
            column: "Payee");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_PaymentMethod",
            table: "Transactions",
            column: "PaymentMethod");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_PocketId",
            table: "Transactions",
            column: "PocketId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_UserId",
            table: "Transactions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ValidFrom",
            table: "Transactions",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ValidFrom_ValidTo",
            table: "Transactions",
            columns: new[] { "ValidFrom", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ValidTo",
            table: "Transactions",
            column: "ValidTo");

        migrationBuilder.CreateIndex(
            name: "IX_UserFeedbacks_FeedbackDate",
            table: "UserFeedbacks",
            column: "FeedbackDate");

        migrationBuilder.CreateIndex(
            name: "IX_UserFeedbacks_TransactionId",
            table: "UserFeedbacks",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_UserFeedbacks_UserId",
            table: "UserFeedbacks",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserPrivacySettings_UserId",
            table: "UserPrivacySettings",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AllowanceTransactions");

        migrationBuilder.DropTable(
            name: "AspNetRoleClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserRoles");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "Assets");

        migrationBuilder.DropTable(
            name: "AuditLogEntries");

        migrationBuilder.DropTable(
            name: "AuditLogs");

        migrationBuilder.DropTable(
            name: "BankConnections");

        migrationBuilder.DropTable(
            name: "BillReminders");

        migrationBuilder.DropTable(
            name: "BudgetAlerts");

        migrationBuilder.DropTable(
            name: "BudgetAlertSettings");

        migrationBuilder.DropTable(
            name: "BudgetFreezes");

        migrationBuilder.DropTable(
            name: "CapitalGains");

        migrationBuilder.DropTable(
            name: "CategoryRules");

        migrationBuilder.DropTable(
            name: "ChallengeTemplates");

        migrationBuilder.DropTable(
            name: "CommentLikes");

        migrationBuilder.DropTable(
            name: "CommuteDeductions");

        migrationBuilder.DropTable(
            name: "CreditRatings");

        migrationBuilder.DropTable(
            name: "CurrencyAccounts");

        migrationBuilder.DropTable(
            name: "Dividends");

        migrationBuilder.DropTable(
            name: "DoNotDisturbSchedules");

        migrationBuilder.DropTable(
            name: "ExpenseShares");

        migrationBuilder.DropTable(
            name: "GoalMilestones");

        migrationBuilder.DropTable(
            name: "GoalShares");

        migrationBuilder.DropTable(
            name: "HouseholdRoles");

        migrationBuilder.DropTable(
            name: "InvestmentTransactions");

        migrationBuilder.DropTable(
            name: "LifeTimelineMilestones");

        migrationBuilder.DropTable(
            name: "LifeTimelineScenarios");

        migrationBuilder.DropTable(
            name: "Loans");

        migrationBuilder.DropTable(
            name: "MLModels");

        migrationBuilder.DropTable(
            name: "NetWorthSnapshots");

        migrationBuilder.DropTable(
            name: "NotificationIntegrations");

        migrationBuilder.DropTable(
            name: "NotificationPreferences");

        migrationBuilder.DropTable(
            name: "Notifications");

        migrationBuilder.DropTable(
            name: "Pensions");

        migrationBuilder.DropTable(
            name: "PocketTransactions");

        migrationBuilder.DropTable(
            name: "PortfolioAllocations");

        migrationBuilder.DropTable(
            name: "ReceiptLineItems");

        migrationBuilder.DropTable(
            name: "Reminders");

        migrationBuilder.DropTable(
            name: "ReminderSettings");

        migrationBuilder.DropTable(
            name: "RolePermissions");

        migrationBuilder.DropTable(
            name: "RoundUpSettings");

        migrationBuilder.DropTable(
            name: "RoundUpTransactions");

        migrationBuilder.DropTable(
            name: "SalaryHistories");

        migrationBuilder.DropTable(
            name: "SavingsChallengeProgress");

        migrationBuilder.DropTable(
            name: "SavingsGroupMembers");

        migrationBuilder.DropTable(
            name: "SharedGoalNotifications");

        migrationBuilder.DropTable(
            name: "SharedGoalParticipants");

        migrationBuilder.DropTable(
            name: "SharedGoalProposalVotes");

        migrationBuilder.DropTable(
            name: "SharedGoalTransactions");

        migrationBuilder.DropTable(
            name: "SubscriptionPriceHistory");

        migrationBuilder.DropTable(
            name: "TaxDeductions");

        migrationBuilder.DropTable(
            name: "TransactionCategories");

        migrationBuilder.DropTable(
            name: "UserFeedbacks");

        migrationBuilder.DropTable(
            name: "UserPrivacySettings");

        migrationBuilder.DropTable(
            name: "AllowanceTasks");

        migrationBuilder.DropTable(
            name: "AspNetRoles");

        migrationBuilder.DropTable(
            name: "Bills");

        migrationBuilder.DropTable(
            name: "BudgetCategories");

        migrationBuilder.DropTable(
            name: "GroupComments");

        migrationBuilder.DropTable(
            name: "SharedExpenses");

        migrationBuilder.DropTable(
            name: "Investments");

        migrationBuilder.DropTable(
            name: "Receipts");

        migrationBuilder.DropTable(
            name: "SavingsChallenges");

        migrationBuilder.DropTable(
            name: "SharedGoalProposals");

        migrationBuilder.DropTable(
            name: "Subscriptions");

        migrationBuilder.DropTable(
            name: "ChildAllowances");

        migrationBuilder.DropTable(
            name: "Budgets");

        migrationBuilder.DropTable(
            name: "GroupGoals");

        migrationBuilder.DropTable(
            name: "Transactions");

        migrationBuilder.DropTable(
            name: "SharedGoals");

        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropTable(
            name: "Goals");

        migrationBuilder.DropTable(
            name: "SavingsGroups");

        migrationBuilder.DropTable(
            name: "Pockets");

        migrationBuilder.DropTable(
            name: "BankSources");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "HouseholdMembers");

        migrationBuilder.DropTable(
            name: "Households");
    }
}

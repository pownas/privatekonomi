# Household Transaction Linkage Implementation

## Overview
This document describes the implementation of household transaction linkage feature that allows users to link transactions to specific households for better expense tracking and reporting.

## Issue Description
**Issue Title:** Koppla transaktioner till specifikt hushålls utgifter

**Requirements:**
- Ability to specify which household a transaction belongs to
- Expenses and reports can be filtered or summarized per household
- Both interface and data model need to support this functionality

## Implementation Details

### 1. Data Model Changes

#### Transaction Model (`Transaction.cs`)
Added a new nullable foreign key property to link transactions to households:

```csharp
/// <summary>
/// Link to household if this transaction belongs to a specific household
/// Null value indicates a personal transaction not linked to any household
/// </summary>
public int? HouseholdId { get; set; }

public Household? Household { get; set; }
```

**Design Decision:** The field is nullable to support both household and personal transactions. This allows users to:
- Link transactions to a specific household
- Keep personal transactions without household linkage
- Maintain flexibility in transaction categorization

#### Database Context (`PrivatekonomyContext.cs`)
Added relationship configuration and indexes:

```csharp
entity.HasOne(e => e.Household)
    .WithMany()
    .HasForeignKey(e => e.HouseholdId)
    .OnDelete(DeleteBehavior.SetNull);

entity.HasIndex(e => e.HouseholdId);
entity.HasIndex(e => new { e.HouseholdId, e.Date });
```

**Performance Optimization:**
- Added index on `HouseholdId` for efficient filtering
- Added composite index on `(HouseholdId, Date)` for date-range queries per household
- Set `OnDelete` behavior to `SetNull` to preserve transactions when a household is deleted

### 2. Service Layer Changes

#### ITransactionService Interface
Added two new methods for household-specific transaction retrieval:

```csharp
Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAsync(int householdId);
Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAndDateRangeAsync(
    int householdId, DateTime from, DateTime to);
```

#### TransactionService Implementation
- Implemented household filtering methods with proper user authentication checks
- Updated all existing query methods to include `Household` entity in the result set
- Maintained existing user isolation for security

#### IReportService & ReportService
Updated report generation methods to support optional household filtering:

```csharp
Task<CashFlowReport> GetCashFlowReportAsync(
    DateTime fromDate, DateTime toDate, string groupBy = "month", int? householdId = null);
Task<TrendAnalysis> GetTrendAnalysisAsync(
    int? categoryId, int months = 6, int? householdId = null);
Task<IEnumerable<TopMerchant>> GetTopMerchantsAsync(
    int limit = 10, DateTime? fromDate = null, DateTime? toDate = null, int? householdId = null);
```

### 3. API Layer Changes

#### TransactionsController
Added support for household filtering via query parameters:

**Existing Endpoint Enhancement:**
```
GET /api/transactions?household_id={id}
```

**New Endpoints:**
```
GET /api/transactions/household/{householdId}
GET /api/transactions/household/{householdId}/date-range?from={date}&to={date}
```

### 4. UI Changes

#### EditTransactionDialog.razor
- Added dropdown selector for household selection
- Loaded available households on component initialization
- Displays "Personal transaction" option when no household is selected
- Clear functionality to remove household linkage

**User Experience:**
```
┌─────────────────────────────────────┐
│ Hushåll: [Vår familj ▼] [X]        │
│ Helper text: Välj ett hushåll       │
│ eller lämna tomt för personlig      │
│ transaktion                          │
└─────────────────────────────────────┘
```

#### Transactions.razor
Enhanced the transaction list page with:
1. **New Column:** "Hushåll" column in the transaction table
   - Shows household name with home icon when linked
   - Shows "Personlig" text for personal transactions

2. **Filter Capability:** Dropdown filter in toolbar
   - Filter transactions by specific household
   - Clearable to show all transactions

3. **Visual Indicators:**
   - Household shown with chip and home icon
   - Color-coded to distinguish from other metadata

**UI Layout:**
```
┌────────────────────────────────────────────────────────────────┐
│ [Sök...] [Filtrera efter hushåll ▼]                           │
├────────┬─────────────┬──────┬──────────┬──────────┬─────────┤
│ Datum  │ Beskrivning │ Bank │ Hushåll  │ Kategori │ Belopp  │
├────────┼─────────────┼──────┼──────────┼──────────┼─────────┤
│ 2025.. │ ICA Kvantum │ ICA  │ 🏠 Familj│ Mat      │ -500 kr │
│ 2025.. │ Netflix     │ SEB  │ Personlig│ Nöje     │ -139 kr │
└────────┴─────────────┴──────┴──────────┴──────────┴─────────┘
```

## Testing & Validation

### Build Validation
- ✅ Solution builds successfully without warnings or errors
- ✅ All projects compile cleanly

### Security Scan
- ✅ CodeQL security scan completed with 0 alerts
- ✅ No vulnerabilities detected in the implementation

### Manual Testing Recommendations

When testing this feature, verify:

1. **Transaction Creation/Editing:**
   - [ ] Can create transaction without household (personal transaction)
   - [ ] Can create transaction with household selected
   - [ ] Can edit existing transaction to add household
   - [ ] Can edit existing transaction to remove household
   - [ ] Dropdown shows all available households

2. **Transaction Listing:**
   - [ ] Household column displays correctly
   - [ ] Personal transactions show "Personlig" text
   - [ ] Household filter works correctly
   - [ ] Clear filter shows all transactions
   - [ ] Search works with household data

3. **API Endpoints:**
   - [ ] `GET /api/transactions?household_id=X` filters correctly
   - [ ] `GET /api/transactions/household/{id}` returns household transactions
   - [ ] `GET /api/transactions/household/{id}/date-range` filters by date

4. **Reports:**
   - [ ] Cash flow reports can be filtered by household
   - [ ] Trend analysis respects household filtering
   - [ ] Top merchants report can filter by household

## Database Migration

**Note:** This implementation includes model changes that require a database migration.

If using a persistent database (SQL Server, PostgreSQL, etc.), create and apply a migration:

```bash
# In Privatekonomi.Core project directory
dotnet ef migrations add AddHouseholdToTransaction

# Apply migration
dotnet ef database update
```

For InMemory database (current setup), the changes are applied automatically on application restart.

## Benefits

1. **Expense Tracking:** Users can now track expenses per household
2. **Reporting:** Reports can be generated for specific households
3. **Flexibility:** Personal transactions can coexist with household transactions
4. **Data Integrity:** Relationships are properly maintained with foreign keys
5. **Performance:** Indexes optimize household-based queries

## Future Enhancements

Potential improvements for consideration:

1. **Dashboard Widget:** Add household expense summary to dashboard
2. **Bulk Operations:** Allow bulk assignment of household to multiple transactions
3. **Household Reports Page:** Dedicated page for household-specific reports
4. **Automatic Assignment:** Rules-based automatic household assignment
5. **Multi-Household:** Support for transactions split across multiple households

## Technical Notes

- **User Isolation:** All queries respect user authentication and only return user's own data
- **Cascading Deletes:** Household deletion sets transaction household to null (preserves transactions)
- **Backward Compatibility:** Existing transactions without household continue to work
- **API Consistency:** Household filtering follows existing query parameter patterns

## References

- Issue: #[Issue Number]
- Related Documentation:
  - [FAMILY_COLLABORATION_GUIDE.md](FAMILY_COLLABORATION_GUIDE.md)
  - [ProgramSpecifikation.md](ProgramSpecifikation.md)
# Historisk API-status

> API-route-exemplen i detta dokument är inte tillgängliga i den aktuella
> standardkonfigurationen. Äldre API-controllers är spärrade och returnerar
> normalt HTTP 403. Web-funktioner som använder interna Core-tjänster är ett
> separat flöde. Se [API-guiden](API_GUIDE.md) och
> [säkerhetsguiden](LEGACY_API_SECURITY.md) innan du använder exempel nedan.

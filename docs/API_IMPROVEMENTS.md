# Backend API Improvements

> **Historiskt och inaktuellt som API-referens.** Detta dokument beskriver ett
> tidigare förslag/implementation och listar routes som nu är spärrade som
> standard. Använd inte exemplen som fungerande API-anrop. Aktuell status finns i
> [API-guiden](API_GUIDE.md) och [säkerhetsguiden](LEGACY_API_SECURITY.md).

Detta dokument beskriver de förbättringar som gjorts i backend API:et baserat på den medföljande OpenAPI-specifikationen.

## Nya Endpoints

### 1. Accounts Controller (`/api/accounts`)

En ny controller för att hantera konton (använder `BankSource` som underliggande entitet).

**Endpoints:**
- `GET /api/accounts` - Lista alla konton
- `GET /api/accounts/{id}` - Hämta ett specifikt konto
- `POST /api/accounts` - Skapa ett nytt konto
- `PUT /api/accounts/{id}` - Uppdatera ett konto
- `DELETE /api/accounts/{id}` - Ta bort ett konto

**Exempel:**
```bash
# Hämta alla konton
GET /api/accounts

# Skapa nytt konto
POST /api/accounts
{
  "name": "Min sparkonto",
  "color": "#4CAF50",
  "logo": "https://example.com/logo.png"
}
```

### 2. Reports Controller (`/api/reports`)

Ny controller för rapporter och analys.

**Endpoints:**

#### Nettoförmögenhet över tid
```
GET /api/reports/networth?start_date=2024-01-01&end_date=2024-12-31
```

**Svar:**
```json
{
  "startDate": "2024-01-01T00:00:00",
  "endDate": "2024-12-31T00:00:00",
  "currentNetWorth": 125000.00,
  "dataPoints": [
    {
      "date": "2024-01-01T00:00:00",
      "income": 35000.00,
      "expense": 25000.00,
      "netWorth": 10000.00,
      "cumulativeNetWorth": 10000.00
    },
    ...
  ]
}
```

#### Månatlig sammanfattning
```
GET /api/reports/summary?year=2024&month=10
```

**Svar:**
```json
{
  "year": 2024,
  "month": 10,
  "income": 35000.00,
  "expense": 22500.00,
  "net": 12500.00,
  "transactionCount": 45,
  "topCategories": [
    {
      "categoryId": 1,
      "categoryName": "Mat & Dryck",
      "totalAmount": 5500.00,
      "transactionCount": 15
    },
    ...
  ]
}
```

### 3. Goals Controller (`/api/goals`)

Controller för att hantera sparmål (savings goals).

**Endpoints:**
- `GET /api/goals` - Lista alla sparmål
- `GET /api/goals/{id}` - Hämta ett specifikt sparmål
- `GET /api/goals/active` - Hämta endast aktiva sparmål
- `GET /api/goals/progress` - Hämta total framsteg för alla aktiva sparmål (i procent)
- `POST /api/goals` - Skapa nytt sparmål
- `PUT /api/goals/{id}` - Uppdatera sparmål
- `DELETE /api/goals/{id}` - Ta bort sparmål

**Datamodell:**
```json
{
  "goalId": 1,
  "name": "Semesterresa",
  "description": "Sommarresa till Italien",
  "targetAmount": 50000.00,
  "currentAmount": 12500.00,
  "targetDate": "2025-06-01T00:00:00",
  "createdDate": "2024-10-19T00:00:00",
  "status": "Active",
  "color": "#2196F3"
}
```

**Status:**
- `Active` - Aktivt sparmål
- `Completed` - Avslutat (målet uppnått)
- `Cancelled` - Avbrutet

**Exempel:**
```bash
# Hämta alla sparmål
GET /api/goals

# Skapa nytt sparmål
POST /api/goals
{
  "name": "Ny bil",
  "description": "Spara till en ny elbil",
  "targetAmount": 350000.00,
  "currentAmount": 0,
  "targetDate": "2025-12-31",
  "color": "#4CAF50"
}

# Hämta framsteg
GET /api/goals/progress
# Returnerar: { "progress": 25.5 }
```


## Förbättrade Endpoints

### Transactions Controller

#### Filtrering och paginering
```
GET /api/transactions?account_id=1&category_id=2&start_date=2024-01-01&end_date=2024-12-31&page=1&per_page=50
```

**Nya query-parametrar:**
- `account_id` - Filtrera på konto-ID
- `start_date` - Filtrera från datum
- `end_date` - Filtrera till datum
- `category_id` - Filtrera på kategori-ID
- `page` - Sidnummer (default: 1)
- `per_page` - Antal per sida (default: 50)

**Svar:**
```json
{
  "transactions": [...],
  "page": 1,
  "perPage": 50,
  "totalCount": 150,
  "totalPages": 3
}
```

### Budgets Controller

#### Periodfiltrering
```
GET /api/budgets?period_start=2024-01-01&period_end=2024-12-31
```

**Nya query-parametrar:**
- `period_start` - Filtrera budgetar som slutar efter detta datum
- `period_end` - Filtrera budgetar som börjar före detta datum

## Modellförbättringar

### Transaction Model

Nya fält har lagts till enligt OpenAPI-specifikationen:

```csharp
public class Transaction
{
    // Befintliga fält...
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public bool IsIncome { get; set; }
    public int? BankSourceId { get; set; }
    
    // NYA fält
    public string? Payee { get; set; }              // Betalningsmottagare
    public string Currency { get; set; } = "SEK";   // Valuta (default SEK)
    public List<string> Tags { get; set; } = new(); // Taggar för kategorisering
    public bool Imported { get; set; }              // Markerar om transaktionen är importerad
    
    // Relationer...
}
```

### Category Model

Stöd för hierarkiska kategorier och standardbudget:

```csharp
public class Category
{
    // Befintliga fält...
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    
    // NYA fält
    public int? ParentId { get; set; }                    // Parent-kategori för hierarki
    public decimal? DefaultBudgetMonthly { get; set; }    // Standardbudget per månad
    
    // NYA relationer
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; }
    
    // Befintliga relationer...
}
```

### TransactionCategory Model

Procentuell fördelning för split-kategorisering:

```csharp
public class TransactionCategory
{
    // Befintliga fält...
    public int TransactionCategoryId { get; set; }
    public int TransactionId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    
    // NYTT fält
    public decimal Percentage { get; set; } = 100; // Procent av transaktionen för denna kategori
    
    // Relationer...
}
```

## Service-förbättringar

### IBankSourceService

Utökad med CRUD-operationer:

```csharp
public interface IBankSourceService
{
    Task<IEnumerable<BankSource>> GetAllBankSourcesAsync();
    Task<BankSource?> GetBankSourceByIdAsync(int id);
    
    // NYA metoder
    Task<BankSource> CreateBankSourceAsync(BankSource bankSource);
    Task<BankSource> UpdateBankSourceAsync(BankSource bankSource);
    Task DeleteBankSourceAsync(int id);
}
```

## Databaskonfiguration

Entity Framework Core-konfigurationen har uppdaterats i `PrivatekonomyContext` för att hantera:

- Self-referencing relationship för kategorihierarki
- Precision för nya decimal-fält
- Maxlängd för nya string-fält

## Användningsexempel

### Filtrera transaktioner på konto och period
```bash
curl "http://localhost:5277/api/transactions?account_id=1&start_date=2024-10-01&end_date=2024-10-31&page=1&per_page=20"
```

### Hämta nettoförmögenhet för året
```bash
curl "http://localhost:5277/api/reports/networth?start_date=2024-01-01&end_date=2024-12-31"
```

### Hämta månatlig sammanfattning
```bash
curl "http://localhost:5277/api/reports/summary?year=2024&month=10"
```

### Skapa ett nytt konto
```bash
curl -X POST "http://localhost:5277/api/accounts" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Nordea Sparkonto",
    "color": "#00A9CE",
    "logo": null
  }'
```

### Hämta budgetar för perioden
```bash
curl "http://localhost:5277/api/budgets?period_start=2024-01-01&period_end=2024-12-31"
```

## Framtida förbättringar

Följande funktioner från OpenAPI-specifikationen är inte implementerade ännu:

1. **Autentisering och auktorisering** - JWT Bearer tokens
2. **Goals (Sparmål)** - Komplett implementation med datamodell
3. **Användarhantering** - User-modell och relaterad funktionalitet
4. **Batch-import av transaktioner** - `POST /api/transactions` med array
5. **Valutakonvertering** - Stöd för flera valutor
6. **Soft-delete** - För konton och transaktioner

## Kompatibilitet

Alla ändringar är bakåtkompatibla. Befintliga endpoints fungerar som tidigare, men returnerar nu även nya fält i svaren. Nya fält har default-värden för att säkerställa kompatibilitet.

## Tester

För att testa de nya endpoints:

1. Starta API:et:
```bash
cd src/Privatekonomi.Api
dotnet run
```

2. Öppna Swagger UI:
```
http://localhost:5277/swagger
```

3. Testa endpoints direkt i Swagger UI eller använd curl/Postman.

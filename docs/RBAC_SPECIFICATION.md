# RBAC Kravspecifikation - Rollbaserad Åtkomstkontroll

> Utvecklingsspecifikation, inte bekräftelse på att RBAC är implementerat eller
> aktivt. Kontrollera aktuell kod och tester innan dokumentet används som
> behörighetsbeskrivning.

**Datum:** 2025-11-05  
**Version:** 1.0  
**Status:** Utvecklingsspecifikation  
**Relaterad Issue:** [#197 - Förbättringsförslag 6.2 RBAC](https://github.com/pownas/Privatekonomi/issues/197)

---

## Innehållsförteckning

1. [Översikt](#översikt)
2. [Roller och Behörigheter](#roller-och-behörigheter)
3. [Tekniska Krav](#tekniska-krav)
4. [Delegation och Temporär Access](#delegation-och-temporär-access)
5. [Audit Log](#audit-log)
6. [Edge Cases och Specialfall](#edge-cases-och-specialfall)
7. [Datamodell](#datamodell)
8. [Implementation Guidelines](#implementation-guidelines)
9. [Säkerhetsaspekter](#säkerhetsaspekter)
10. [Testscenarier](#testscenarier)

---

## Översikt

### Syfte

Detta dokument specificerar ett rollbaserat åtkomstkontrollsystem (RBAC) för Privatekonomi som möjliggör:

- Finmaskig kontroll över vem som kan se och redigera data inom ett hushåll
- Säker delning av ekonomisk information mellan familjemedlemmar
- Skydd för barns ekonomiska data med anpassade begränsningar
- Spårbarhet genom audit logging av alla behörighetsändringar
- Flexibel delegation av behörigheter för temporära behov

### Målgrupp

- **Familjer** med flera vuxna som delar ekonomiskt ansvar
- **Föräldrar** som vill ge barn begränsad åtkomst till ekonomidata
- **Äldre** som vill ge anhöriga view-only access för övervakning
- **Administratörer** som hanterar hushållets ekonomi

### Grundprinciper

1. **Least Privilege**: Användare får endast de behörigheter som krävs
2. **Separation of Duties**: Kritiska åtgärder kräver högre behörighetsnivå
3. **Audit Trail**: Alla behörighetsändringar loggas
4. **Transparent**: Användare ser tydligt vilka behörigheter de har
5. **Revocable**: Behörigheter kan återkallas när som helst

---

## Roller och Behörigheter

### Rollhierarki

```
Admin
  ├─ Full Access
  │   ├─ Editor
  │   │   └─ View Only
  │   └─ Limited
  └─ Child (specialroll)
```

### 1. Admin (Hushållsägare)

**Beskrivning:** Fullständig kontroll över hushållet och alla dess data. Endast EN admin per hushåll (kan överföras).

#### Behörigheter

**Hushållshantering:**
- ✅ Skapa/radera hushåll
- ✅ Byta namn och beskrivning på hushåll
- ✅ Lägga till/ta bort medlemmar
- ✅ Tilldela/ändra roller för medlemmar
- ✅ Överföra admin-roll till annan medlem
- ✅ Ta bort hushåll (med bekräftelse)

**Ekonomiska Data:**
- ✅ Skapa/redigera/radera alla transaktioner (egna och andras)
- ✅ Skapa/redigera/radera alla budgetar
- ✅ Skapa/redigera/radera alla sparmål
- ✅ Skapa/redigera/radera delade utgifter
- ✅ Godkänna/avvisa transaktioner från andra medlemmar

**Barnkonton:**
- ✅ Skapa/hantera barnkonton
- ✅ Betala veckopeng
- ✅ Godkänna/avvisa uppdrag
- ✅ Justera saldo

**Inställningar:**
- ✅ Konfigurera notifikationsinställningar för hushåll
- ✅ Hantera integrationsinställningar (PSD2, bankapi)
- ✅ Exportera ALL data från hushållet
- ✅ Se audit log för alla ändringar

**Delegation:**
- ✅ Ge temporär access till alla roller
- ✅ Återkalla delegation

**Begränsningar:**
- ❌ Kan inte radera sin egen admin-roll utan att först överföra den
- ❌ Kan inte lämna hushållet utan att först överföra admin-rollen

---

### 2. Full Access (Delägare)

**Beskrivning:** Nästan lika behörigheter som Admin, men kan inte hantera medlemmar eller roller.

#### Behörigheter

**Hushållshantering:**
- ✅ Se alla medlemmar
- ✅ Se roller för medlemmar
- ❌ Lägga till/ta bort medlemmar
- ❌ Ändra roller
- ❌ Radera hushåll

**Ekonomiska Data:**
- ✅ Skapa/redigera/radera alla transaktioner (egna och andras)
- ✅ Skapa/redigera/radera alla budgetar
- ✅ Skapa/redigera/radera alla sparmål
- ✅ Skapa/redigera/radera delade utgifter
- ⚠️ Godkänna transaktioner från andra medlemmar (upp till 10,000 kr)

**Barnkonton:**
- ✅ Se barnkonton
- ✅ Betala veckopeng
- ✅ Godkänna/avvisa uppdrag
- ⚠️ Justera saldo (upp till ±1,000 kr)

**Inställningar:**
- ✅ Se notifikationsinställningar
- ⚠️ Ändra egna notifikationsinställningar
- ✅ Exportera ekonomiska rapporter
- ✅ Se audit log (endast för egna åtgärder och hushållsnivå)

**Delegation:**
- ❌ Kan inte delegera behörigheter

**Begränsningar:**
- ❌ Kan inte hantera roller
- ❌ Kan inte radera hushåll
- ⚠️ Stora transaktioner (>10,000 kr) kräver admin-godkännande

---

### 3. Editor (Redigerare)

**Beskrivning:** Kan skapa och redigera data, men med begränsningar på stora belopp.

#### Behörigheter

**Hushållshantering:**
- ✅ Se alla medlemmar
- ✅ Se roller för medlemmar
- ❌ Lägga till/ta bort medlemmar
- ❌ Ändra roller

**Ekonomiska Data:**
- ✅ Skapa/redigera **egna** transaktioner
- ⚠️ Redigera andras transaktioner (upp till 1,000 kr, kräver godkännande)
- ✅ Skapa/redigera **egna** budgetar
- ✅ Se alla budgetar
- ✅ Skapa/redigera **egna** sparmål
- ✅ Se alla sparmål
- ✅ Skapa delade utgifter (upp till 5,000 kr)
- ❌ Radera andras transaktioner

**Barnkonton:**
- ✅ Se barnkonton
- ⚠️ Betala veckopeng (upp till schemat belopp)
- ⚠️ Godkänna uppdrag (upp till 500 kr)
- ❌ Justera saldo

**Inställningar:**
- ✅ Se notifikationsinställningar
- ⚠️ Ändra egna notifikationsinställningar
- ✅ Exportera egna rapporter
- ✅ Se audit log (endast för egna åtgärder)

**Delegation:**
- ❌ Kan inte delegera behörigheter

**Begränsningar:**
- ⚠️ Transaktioner >5,000 kr kräver godkännande från Admin/Full Access
- ⚠️ Redigering av andras transaktioner kräver godkännande
- ❌ Kan inte radera hushållsdata

---

### 4. View Only (Granskare)

**Beskrivning:** Kan endast se data, ingen redigeringsmöjlighet. Perfekt för äldre anhöriga eller ekonomisk rådgivare.

#### Behörigheter

**Hushållshantering:**
- ✅ Se alla medlemmar
- ✅ Se roller för medlemmar
- ❌ Alla ändringar

**Ekonomiska Data:**
- ✅ Se alla transaktioner
- ✅ Se alla budgetar
- ✅ Se alla sparmål
- ✅ Se delade utgifter
- ✅ Se alla rapporter och grafer
- ❌ Skapa/redigera/radera någonting

**Barnkonton:**
- ✅ Se barnkonton
- ✅ Se transaktionshistorik
- ✅ Se uppdrag och status
- ❌ Någon ändring

**Inställningar:**
- ✅ Se notifikationsinställningar (read-only)
- ✅ Exportera rapporter (PDF, Excel)
- ✅ Se audit log (endast för hushållsnivå)

**Delegation:**
- ❌ Kan inte delegera behörigheter

**Begränsningar:**
- ❌ Kan INTE redigera, skapa eller radera något
- ❌ Kan INTE godkänna transaktioner eller uppdrag
- ✅ Kan endast se och exportera data

---

### 5. Limited (Begränsad)

**Beskrivning:** Kan endast se och hantera **egna** transaktioner och budgetar. Kan inte se andras data.

#### Behörigheter

**Hushållshantering:**
- ✅ Se att hushållet existerar
- ✅ Se sitt eget medlemskap
- ❌ Se andra medlemmar (utom admin)
- ❌ Se roller

**Ekonomiska Data:**
- ✅ Se **endast egna** transaktioner
- ✅ Skapa/redigera/radera **egna** transaktioner (upp till 2,000 kr)
- ✅ Se **endast egna** budgetar
- ✅ Skapa/redigera **egna** budgetar
- ✅ Se **endast egna** sparmål
- ❌ Se andras transaktioner/budgetar/mål
- ❌ Se delade utgifter (förutom egna andelar)

**Barnkonton:**
- ❌ Kan inte se barnkonton

**Inställningar:**
- ✅ Ändra egna notifikationsinställningar
- ✅ Exportera egna rapporter
- ❌ Se audit log

**Delegation:**
- ❌ Kan inte delegera behörigheter

**Begränsningar:**
- ⚠️ Transaktioner >2,000 kr kräver godkännande
- ❌ Kan INTE se andras ekonomiska data
- ❌ Kan INTE se hushållsövergripande statistik

---

### 6. Child (Barn - Specialroll)

**Beskrivning:** Specialroll för barn med strikta begränsningar och föräldrakontroll.

#### Behörigheter

**Hushållshantering:**
- ✅ Se att hushållet existerar
- ✅ Se föräldrar (admin)
- ❌ Se andra medlemmar
- ❌ Lämna hushåll

**Ekonomiska Data:**
- ✅ Se **endast sitt eget barnkonto**
- ✅ Se sitt saldo
- ✅ Se transaktionshistorik (veckopeng, belöningar, uttag)
- ❌ Skapa transaktioner (utom uttag från eget konto med godkännande)
- ❌ Se hushållets övriga ekonomi

**Barnkonton:**
- ✅ Se egna uppdrag
- ✅ Markera uppdrag som klara
- ⚠️ Begära uttag (kräver föräldragodkännande)
- ✅ Skapa egna sparmål (inom barnkonto)
- ❌ Se andra barns konton

**Inställningar:**
- ⚠️ Begränsade notifikationsinställningar (endast in-app)
- ❌ Exportera data
- ❌ Se audit log

**Delegation:**
- ❌ Kan inte delegera behörigheter

**Åldersberoende Begränsningar:**

**Barn <13 år:**
- ❌ Kan inte logga in i webbapp (endast förälder ser kontot)
- ❌ Inga självständiga åtgärder

**Barn 13-15 år:**
- ✅ Kan logga in
- ✅ Se eget konto
- ✅ Markera uppdrag som klara
- ⚠️ Uttag kräver alltid godkännande

**Barn 16-17 år:**
- ✅ Allt ovan +
- ✅ Uttag upp till 500 kr/vecka utan godkännande
- ✅ Skapa egna sparmål

**Automatisk Uppgradering:**
- Vid 18 år: Automatiskt uppgraderad till "Limited" roll
- Notifikation skickas till barn och föräldrar

---

## Tekniska Krav

### 1. Databasschema

#### Ny Tabell: HouseholdRole

```csharp
public class HouseholdRole
{
    public int HouseholdRoleId { get; set; }
    public int HouseholdMemberId { get; set; }
    public HouseholdRoleType RoleType { get; set; }
    
    // Delegering
    public bool IsDelegated { get; set; }
    public string? DelegatedBy { get; set; }  // UserId som delegerade
    public DateTime? DelegationStartDate { get; set; }
    public DateTime? DelegationEndDate { get; set; }
    
    // Metadata
    public DateTime AssignedDate { get; set; }
    public string? AssignedBy { get; set; }  // UserId
    public DateTime? RevokedDate { get; set; }
    public string? RevokedBy { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public HouseholdMember? HouseholdMember { get; set; }
}

public enum HouseholdRoleType
{
    Admin = 1,
    FullAccess = 2,
    Editor = 3,
    ViewOnly = 4,
    Limited = 5,
    Child = 6
}
```

#### Uppdatera HouseholdMember

```csharp
public class HouseholdMember
{
    // ... befintliga fält
    
    // RBAC
    public ICollection<HouseholdRole> Roles { get; set; } = new List<HouseholdRole>();
    public DateTime? DateOfBirth { get; set; }  // För ålderskontroll
    public int? Age => DateOfBirth.HasValue 
        ? DateTime.Now.Year - DateOfBirth.Value.Year 
        : null;
}
```

#### Ny Tabell: RolePermission (för framtida utökning)

```csharp
public class RolePermission
{
    public int RolePermissionId { get; set; }
    public HouseholdRoleType RoleType { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public decimal? AmountLimit { get; set; }  // Null = ingen gräns
}
```

**Standard Permissions:**
```
household.view
household.edit
household.delete
household.manage_members
household.manage_roles

transaction.view.own
transaction.view.all
transaction.create
transaction.edit.own
transaction.edit.all
transaction.delete.own
transaction.delete.all
transaction.approve

budget.view.own
budget.view.all
budget.create
budget.edit.own
budget.edit.all
budget.delete.own
budget.delete.all

goal.view.own
goal.view.all
goal.create
goal.edit.own
goal.edit.all

child_account.view
child_account.create
child_account.edit
child_account.manage_tasks
child_account.approve_tasks
child_account.pay_allowance

delegation.grant
delegation.revoke

audit.view.own
audit.view.all
```

---

### 2. Authorization Service

#### IRbacService Interface

```csharp
public interface IRbacService
{
    // Rollkontroll
    Task<HouseholdRole?> GetUserRoleInHouseholdAsync(string userId, int householdId);
    Task<bool> HasRoleAsync(string userId, int householdId, HouseholdRoleType roleType);
    Task<bool> HasMinimumRoleAsync(string userId, int householdId, HouseholdRoleType minimumRole);
    
    // Behörighetskontroll
    Task<bool> HasPermissionAsync(string userId, int householdId, string permissionKey);
    Task<bool> CanPerformActionAsync(string userId, int householdId, string action, decimal? amount = null);
    
    // Delegering
    Task<HouseholdRole> DelegateRoleAsync(string delegatorUserId, string targetUserId, 
        int householdId, HouseholdRoleType roleType, DateTime? endDate);
    Task<bool> RevokeDelegationAsync(string userId, int delegationId);
    Task<IEnumerable<HouseholdRole>> GetActiveDelegationsAsync(int householdId);
    
    // Rollhantering
    Task<HouseholdRole> AssignRoleAsync(string assignerUserId, int householdMemberId, 
        HouseholdRoleType roleType);
    Task<bool> RemoveRoleAsync(string userId, int roleId);
    Task<HouseholdRole> TransferAdminRoleAsync(string currentAdminUserId, string newAdminUserId, 
        int householdId);
    
    // Validering
    Task<RoleValidationResult> ValidateRoleAssignmentAsync(string userId, int householdId, 
        int targetMemberId, HouseholdRoleType roleType);
    Task<PermissionCheckResult> CheckPermissionAsync(string userId, int householdId, 
        string permissionKey, object? context = null);
}

public class PermissionCheckResult
{
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }
    public decimal? AmountLimit { get; set; }
    public bool RequiresApproval { get; set; }
}

public class RoleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
```

---

### 3. Authorization Attributes

#### Custom Authorize Attribute

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireHouseholdRoleAttribute : Attribute
{
    public HouseholdRoleType MinimumRole { get; set; }
    public string? Permission { get; set; }
    
    public RequireHouseholdRoleAttribute(HouseholdRoleType minimumRole)
    {
        MinimumRole = minimumRole;
    }
}

// Användning:
[RequireHouseholdRole(HouseholdRoleType.Admin)]
public async Task DeleteHouseholdAsync(int householdId) { }

[RequireHouseholdRole(HouseholdRoleType.Editor)]
public async Task CreateTransactionAsync(Transaction transaction) { }
```

---

### 4. Blazor Component Integration

#### AuthorizeView för UI

```razor
<AuthorizeView Policy="@GetHouseholdPolicy(HouseholdRoleType.Admin)">
    <Authorized>
        <MudButton OnClick="DeleteHousehold" Color="Color.Error">
            Radera hushåll
        </MudButton>
    </Authorized>
    <NotAuthorized>
        <MudTooltip Text="Endast admin kan radera hushåll">
            <MudButton Disabled="true" Color="Color.Error">
                Radera hushåll
            </MudButton>
        </MudTooltip>
    </NotAuthorized>
</AuthorizeView>
```

#### Permission Check i Code-Behind

```csharp
protected override async Task OnInitializedAsync()
{
    _currentRole = await RbacService.GetUserRoleInHouseholdAsync(CurrentUser.UserId, HouseholdId);
    _canEditTransactions = await RbacService.HasPermissionAsync(
        CurrentUser.UserId, HouseholdId, "transaction.edit.all");
}
```

---

## Delegation och Temporär Access

### Användningsfall

1. **Semestervikarie:** Admin ger Full Access till partner under semester (2 veckor)
2. **Tillfällig hjälp:** Full Access ger Editor-rättigheter till ekonomisk rådgivare (1 månad)
3. **Nödsituation:** Admin ger tillfällig Full Access till anhörig vid sjukdom

### Delegationsregler

#### Vem kan delegera vad?

| Delegator Roll | Kan delegera roll | Max delegationsperiod | Kräver godkännande |
|----------------|------------------|----------------------|-------------------|
| Admin | Admin | 30 dagar | ❌ Nej |
| Admin | Full Access | 90 dagar | ❌ Nej |
| Admin | Editor | 90 dagar | ❌ Nej |
| Admin | View Only | 365 dagar | ❌ Nej |
| Admin | Limited | 90 dagar | ❌ Nej |
| Full Access | Editor | 30 dagar | ✅ Admin måste godkänna |
| Full Access | View Only | 90 dagar | ❌ Nej |
| Full Access | Limited | 30 dagar | ✅ Admin måste godkänna |
| Andra | - | - | ❌ Kan inte delegera |

#### Delegationsbegränsningar

1. **Ingen delegationskedja:** En delegerad roll kan inte delegera vidare
2. **Automatisk återkallelse:** Delegation upphör automatiskt vid slutdatum
3. **Manual återkallelse:** Delegator eller Admin kan återkalla när som helst
4. **Loggning:** Alla delegeringar loggas i audit log
5. **Notifikationer:** Både delegator och mottagare får notifikation om start/slut

### Teknisk Implementation

```csharp
public async Task<HouseholdRole> DelegateRoleAsync(
    string delegatorUserId, 
    string targetUserId, 
    int householdId, 
    HouseholdRoleType roleType, 
    DateTime? endDate)
{
    // 1. Validera att delegator har rätt att delegera
    var delegatorRole = await GetUserRoleInHouseholdAsync(delegatorUserId, householdId);
    if (!CanDelegate(delegatorRole.RoleType, roleType))
        throw new UnauthorizedAccessException("Du har inte behörighet att delegera denna roll");
    
    // 2. Validera slutdatum
    var maxPeriod = GetMaxDelegationPeriod(delegatorRole.RoleType, roleType);
    if (endDate > DateTime.Now.AddDays(maxPeriod))
        throw new ArgumentException($"Max delegationsperiod är {maxPeriod} dagar");
    
    // 3. Kräver godkännande?
    var requiresApproval = RequiresApprovalForDelegation(delegatorRole.RoleType, roleType);
    
    // 4. Skapa delegerad roll
    var delegatedRole = new HouseholdRole
    {
        HouseholdMemberId = GetMemberId(targetUserId, householdId),
        RoleType = roleType,
        IsDelegated = true,
        DelegatedBy = delegatorUserId,
        DelegationStartDate = DateTime.Now,
        DelegationEndDate = endDate ?? DateTime.Now.AddDays(maxPeriod),
        AssignedDate = DateTime.Now,
        AssignedBy = delegatorUserId,
        IsActive = !requiresApproval  // Väntar på godkännande om required
    };
    
    await _context.HouseholdRoles.AddAsync(delegatedRole);
    
    // 5. Logga delegation
    await _auditService.LogAsync(new AuditLogEntry
    {
        Action = "RoleDelegated",
        UserId = delegatorUserId,
        HouseholdId = householdId,
        Details = $"Delegated {roleType} to user {targetUserId} until {endDate}",
        Severity = AuditSeverity.High
    });
    
    // 6. Skicka notifikationer
    await _notificationService.NotifyDelegationCreatedAsync(delegatedRole);
    
    // 7. Schemalägg automatisk återkallelse
    await _backgroundJobService.ScheduleAsync<RevokeDelegationJob>(
        delegatedRole.HouseholdRoleId, 
        delegatedRole.DelegationEndDate.Value);
    
    await _context.SaveChangesAsync();
    return delegatedRole;
}
```

---

## Audit Log

### Loggade Händelser

#### Kategori: Roll och Behörigheter

| Händelse | Severity | Data som loggas | Retention |
|----------|----------|----------------|-----------|
| RoleAssigned | High | UserId, TargetMemberId, RoleType, AssignedBy | 5 år |
| RoleRevoked | High | UserId, RoleId, RevokedBy, Reason | 5 år |
| RoleChanged | High | UserId, OldRole, NewRole, ChangedBy | 5 år |
| AdminTransferred | Critical | OldAdminId, NewAdminId, HouseholdId | Permanent |
| RoleDelegated | High | DelegatorId, TargetId, RoleType, EndDate | 2 år |
| DelegationRevoked | Medium | DelegationId, RevokedBy, Reason | 2 år |
| DelegationExpired | Low | DelegationId, RoleType | 1 år |
| DelegationApproved | Medium | DelegationId, ApprovedBy | 2 år |
| DelegationDenied | Medium | DelegationId, DeniedBy, Reason | 2 år |

#### Kategori: Åtkomstförsök

| Händelse | Severity | Data som loggas | Retention |
|----------|----------|----------------|-----------|
| PermissionGranted | Low | UserId, Permission, Resource | 90 dagar |
| PermissionDenied | Medium | UserId, Permission, Resource, Reason | 1 år |
| UnauthorizedAccess | High | UserId, Resource, AttemptedAction | 2 år |
| ElevatedActionPerformed | Medium | UserId, Action, Amount, ApprovedBy | 2 år |

#### Kategori: Medlemskap

| Händelse | Severity | Data som loggas | Retention |
|----------|----------|----------------|-----------|
| MemberAdded | Medium | MemberId, AddedBy, InitialRole | 5 år |
| MemberRemoved | High | MemberId, RemovedBy, Reason | 5 år |
| MemberLeft | Medium | MemberId, LeftDate | 5 år |
| ChildAgeUpdated | Low | ChildMemberId, NewAge, AutoRoleChange | 2 år |

### Datamodell

```csharp
public class AuditLogEntry
{
    public int AuditLogEntryId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Vem
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    
    // Vad
    public string Action { get; set; } = string.Empty;
    public AuditCategory Category { get; set; }
    public AuditSeverity Severity { get; set; }
    
    // Var
    public int? HouseholdId { get; set; }
    public string? ResourceType { get; set; }
    public int? ResourceId { get; set; }
    
    // Detaljer
    public string? Details { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    
    // Metadata
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum AuditCategory
{
    RoleAndPermission,
    AccessAttempt,
    Membership,
    DataModification,
    SystemEvent
}

public enum AuditSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

### Visning i UI

```razor
<MudDataGrid Items="@_auditLogs" Filterable="true" FilterMode="DataGridFilterMode.ColumnFilterRow">
    <Columns>
        <PropertyColumn Property="x => x.Timestamp" Title="Tidpunkt" Format="yyyy-MM-dd HH:mm:ss" />
        <PropertyColumn Property="x => x.UserName" Title="Användare" />
        <PropertyColumn Property="x => x.Action" Title="Händelse" />
        <TemplateColumn Title="Severity">
            <CellTemplate>
                <MudChip Size="Size.Small" Color="@GetSeverityColor(context.Item.Severity)">
                    @context.Item.Severity
                </MudChip>
            </CellTemplate>
        </TemplateColumn>
        <PropertyColumn Property="x => x.Details" Title="Detaljer" />
    </Columns>
</MudDataGrid>
```

---

## Edge Cases och Specialfall

### 1. Byte av Hushållsmedlem

#### Scenario: Vuxen lämnar hushåll

**Problem:** Vad händer med deras transaktioner, budgetar och delegerade rättigheter?

**Lösning:**
1. Medlem sätts till `IsActive = false`
2. `LeftDate` sätts
3. Alla aktiva delegationer återkallas automatiskt
4. Roll sätts till inaktiv men raderas INTE
5. Historiska transaktioner behålls men associeras med "Före detta medlem"
6. Delade utgifter frysas på nuvarande fördelning
7. Audit log-entry skapas

```csharp
public async Task RemoveMemberAsync(int householdId, int memberId, string removedByUserId)
{
    var member = await _context.HouseholdMembers.FindAsync(memberId);
    if (member == null) throw new NotFoundException();
    
    // 1. Inaktivera medlem
    member.IsActive = false;
    member.LeftDate = DateTime.Now;
    
    // 2. Återkalla alla roller och delegationer
    var roles = await _context.HouseholdRoles
        .Where(r => r.HouseholdMemberId == memberId && r.IsActive)
        .ToListAsync();
    
    foreach (var role in roles)
    {
        role.IsActive = false;
        role.RevokedDate = DateTime.Now;
        role.RevokedBy = removedByUserId;
    }
    
    // 3. Återkalla alla FRÅN denna medlem delegerade rättigheter
    var delegations = await _context.HouseholdRoles
        .Where(r => r.DelegatedBy == member.UserId && r.IsDelegated && r.IsActive)
        .ToListAsync();
    
    foreach (var delegation in delegations)
    {
        delegation.IsActive = false;
        delegation.RevokedDate = DateTime.Now;
        delegation.RevokedBy = removedByUserId;
    }
    
    // 4. Frysa delade utgifter
    var activeExpenses = await _context.SharedExpenses
        .Include(e => e.ExpenseShares)
        .Where(e => e.HouseholdId == householdId && 
                    e.ExpenseShares.Any(s => s.HouseholdMemberId == memberId))
        .ToListAsync();
    
    foreach (var expense in activeExpenses)
    {
        // Markera som "fryst" eller omfördela
        expense.IsFrozen = true;
    }
    
    // 5. Logga
    await _auditService.LogAsync(new AuditLogEntry
    {
        Action = "MemberRemoved",
        UserId = removedByUserId,
        HouseholdId = householdId,
        Details = $"Member {member.Name} removed from household",
        Severity = AuditSeverity.High
    });
    
    await _context.SaveChangesAsync();
}
```

#### Scenario: Admin lämnar hushåll

**Problem:** Hushållet kan inte fungera utan admin.

**Lösning:**
1. Admin MÅSTE först överföra admin-rollen till någon annan
2. Om inga andra medlemmar finns: Hushållet RADERAS (med bekräftelse)
3. System blockerar "lämna hushåll" för admin tills transfer är gjord

---

### 2. Barntillgång och Åldersövergångar

#### Scenario: Barn fyller år

**Åtgärder:**

**Vid 13 år:**
- Notifikation till föräldrar: "X kan nu få inloggning"
- Föräldrar kan välja att aktivera login för barnet
- Om aktiverad: Temporärt lösenord genereras och skickas till föräldrar

**Vid 18 år:**
- Automatisk uppgradering från Child → Limited
- Notifikation till både barn och föräldrar
- Barnkonto blir "vuxenkonto" (behåller historik)
- Barn kan nu själv ändra sin roll (med Admin-godkännande)

```csharp
// Background job som körs dagligen
public class CheckChildAgeTransitionsJob
{
    public async Task ExecuteAsync()
    {
        var today = DateTime.Today;
        
        // Hitta barn som fyller 13 idag
        var turning13 = await _context.HouseholdMembers
            .Include(m => m.Roles)
            .Where(m => m.DateOfBirth.HasValue && 
                       m.DateOfBirth.Value.AddYears(13).Date == today &&
                       m.Roles.Any(r => r.RoleType == HouseholdRoleType.Child && r.IsActive))
            .ToListAsync();
        
        foreach (var child in turning13)
        {
            await _notificationService.NotifyChildAge13Async(child);
        }
        
        // Hitta barn som fyller 18 idag
        var turning18 = await _context.HouseholdMembers
            .Include(m => m.Roles)
            .Where(m => m.DateOfBirth.HasValue && 
                       m.DateOfBirth.Value.AddYears(18).Date == today &&
                       m.Roles.Any(r => r.RoleType == HouseholdRoleType.Child && r.IsActive))
            .ToListAsync();
        
        foreach (var child in turning18)
        {
            await UpgradeChildToLimitedAsync(child);
        }
    }
    
    private async Task UpgradeChildToLimitedAsync(HouseholdMember member)
    {
        // Inaktivera Child-roll
        var childRole = member.Roles.First(r => r.RoleType == HouseholdRoleType.Child && r.IsActive);
        childRole.IsActive = false;
        childRole.RevokedDate = DateTime.Now;
        childRole.RevokedBy = "SYSTEM";
        
        // Skapa Limited-roll
        var limitedRole = new HouseholdRole
        {
            HouseholdMemberId = member.HouseholdMemberId,
            RoleType = HouseholdRoleType.Limited,
            AssignedDate = DateTime.Now,
            AssignedBy = "SYSTEM",
            IsActive = true
        };
        
        _context.HouseholdRoles.Add(limitedRole);
        
        // Logga
        await _auditService.LogAsync(new AuditLogEntry
        {
            Action = "ChildAgeUpgrade",
            UserId = member.UserId,
            HouseholdId = member.HouseholdId,
            Details = $"Child {member.Name} turned 18, upgraded to Limited role",
            Severity = AuditSeverity.Medium
        });
        
        // Notifiera
        await _notificationService.NotifyRoleUpgradeAsync(member, HouseholdRoleType.Limited);
        
        await _context.SaveChangesAsync();
    }
}
```

---

### 3. Temporär Access - Vacation Mode

#### Scenario: Förälder på semester

**Behov:** Ge partner tillfällig Full Access för att hantera allt under 2 veckor.

**Implementation:**
```csharp
// I UI: Household Settings → Delegation
var delegation = await _rbacService.DelegateRoleAsync(
    delegatorUserId: currentUser.Id,
    targetUserId: partner.UserId,
    householdId: householdId,
    roleType: HouseholdRoleType.FullAccess,
    endDate: DateTime.Now.AddDays(14)
);

// Automatisk notifikation när delegation skapas
await _notificationService.SendAsync(new Notification
{
    UserId = partner.UserId,
    Type = NotificationType.RoleDelegated,
    Title = "Du har fått Full Access",
    Message = $"{currentUser.Name} har gett dig Full Access till {household.Name} fram till {delegation.DelegationEndDate:yyyy-MM-dd}",
    Severity = NotificationSeverity.Info
});

// Automatisk notifikation 24h innan delegation upphör
await _backgroundJobService.ScheduleAsync<SendDelegationExpiryReminderJob>(
    delegation.HouseholdRoleId,
    delegation.DelegationEndDate.Value.AddDays(-1)
);

// Automatisk återkallelse vid slutdatum
await _backgroundJobService.ScheduleAsync<RevokeDelegationJob>(
    delegation.HouseholdRoleId,
    delegation.DelegationEndDate.Value
);
```

---

### 4. Ekonomisk Rådgivare (Temporary View-Only)

#### Scenario: Familj anlitar ekonomisk rådgivare

**Lösning:**
1. Admin skapar en "gästmedlem" med View Only-roll
2. Temporärt lösenord genereras
3. Delegation sätts till 30 dagar
4. Rådgivaren kan logga in och se all ekonomi
5. Efter 30 dagar: Automatisk återkallelse
6. All aktivitet loggas

---

### 5. Multi-Household Scenario

#### Scenario: Person tillhör flera hushåll

**Problem:** Anna är Admin i "Familjen Andersson" men Limited i "Sommarstuga-kollektivet".

**Lösning:**
- Roller är **per-household**
- När Anna öppnar "Familjen Andersson": Ser Admin-UI
- När Anna öppnar "Sommarstuga-kollektivet": Ser Limited-UI
- Context switch i UI visar aktuell roll tydligt

```razor
<MudAppBar>
    <MudText>@_currentHousehold.Name</MudText>
    <MudChip Color="@GetRoleColor(_currentRole)">
        @GetRoleDisplayName(_currentRole)
    </MudChip>
</MudAppBar>
```

---

### 6. Konflikthantering

#### Scenario: Två Admin-roller finns (datafel)

**Prevention:**
- Database constraint: Endast EN aktiv Admin per hushåll
- Validering i service layer

```csharp
public async Task<bool> ValidateHouseholdRolesAsync(int householdId)
{
    var activeAdmins = await _context.HouseholdRoles
        .Where(r => r.HouseholdMember.HouseholdId == householdId &&
                   r.RoleType == HouseholdRoleType.Admin &&
                   r.IsActive)
        .CountAsync();
    
    if (activeAdmins == 0)
    {
        throw new InvalidOperationException("Household must have exactly one Admin");
    }
    
    if (activeAdmins > 1)
    {
        // Logga som kritiskt fel
        await _auditService.LogAsync(new AuditLogEntry
        {
            Action = "MultipleAdminsDetected",
            HouseholdId = householdId,
            Severity = AuditSeverity.Critical
        });
        
        throw new DataIntegrityException("Multiple active Admins detected");
    }
    
    return true;
}
```

---

## Säkerhetsaspekter

### 1. Principle of Least Privilege

- Nya medlemmar får automatiskt **Limited** roll
- Uppgradering kräver explicit åtgärd av Admin
- Delegationer är tidsbegränsade som standard

### 2. Defense in Depth

**Lager 1: UI-nivå**
- Knappar/fält disableas baserat på roll
- AuthorizeView döljer känsliga sektioner

**Lager 2: Component-nivå**
- Validering i OnInitializedAsync
- RedirectTo om otillräckliga rättigheter

**Lager 3: Service-nivå**
- Alla metoder kollar behörighet
- Exception kastas vid otillåtna åtgärder

**Lager 4: API-nivå**
- [Authorize] attribut på controllers
- Custom policy enforcement

**Lager 5: Database-nivå**
- Row-level security (framtida)
- Constraints för data integrity

### 3. Audit Trail

- Alla RBAC-ändringar loggas
- Loggarna är **immutable** (kan inte redigeras)
- Retention policy: Minimum 1 år, upp till permanent

### 4. Separation of Duties

- Admin kan inte godkänna sina egna delegeringar
- Stora transaktioner kräver tvåpersonsregel
- Rollbyten kräver explicit godkännande

### 5. Secure Defaults

```csharp
public class RbacDefaults
{
    public const HouseholdRoleType DefaultNewMemberRole = HouseholdRoleType.Limited;
    public const int DefaultDelegationDays = 30;
    public const decimal DefaultTransactionLimit = 5000m;
    public const bool RequireApprovalForLargeTransactions = true;
}
```

---

## Implementation Guidelines

### Fas 1: Foundation (Vecka 1-2)

**Mål:** Grundläggande RBAC-infrastruktur

1. **Datamodell**
   - Skapa `HouseholdRole` tabell
   - Skapa `RolePermission` tabell
   - Skapa `AuditLogEntry` tabell
   - Migration och seeding

2. **Core Services**
   - Implementera `IRbacService`
   - Implementera `IAuditService`
   - Unit tests för services

3. **Authorization Policies**
   - Definiera policies i Program.cs
   - Custom authorization handlers

**Leverabler:**
- ✅ Databastabeller skapade
- ✅ Services implementerade och testade
- ✅ Policies konfigurerade

---

### Fas 2: Basic Roles (Vecka 3-4)

**Mål:** Implementera Admin, Full Access, View Only

1. **UI Components**
   - RoleManagement.razor component
   - RoleAssignment dialog
   - Permission indicators

2. **Integration**
   - Uppdatera HouseholdDetails.razor
   - Lägg till rollkontroller i Transactions
   - Lägg till rollkontroller i Budgets

3. **Testing**
   - Manual testing av alla roller
   - Edge case testing

**Leverabler:**
- ✅ 3 grundroller fungerar
- ✅ UI visar korrekt baserat på roll
- ✅ Tester passed

---

### Fas 3: Child & Limited Roles (Vecka 5)

**Mål:** Specialroller för barn och begränsade användare

1. **Child Role**
   - Åldersvalidering
   - Barnkonto-integration
   - Uppdrags-workflow

2. **Limited Role**
   - Data isolation
   - Own-only queries

3. **Age Transitions**
   - Background job för ålderskontroll
   - Automatisk uppgradering

**Leverabler:**
- ✅ Child och Limited roller fungerar
- ✅ Ålders-transitions automatiserade
- ✅ Tester passed

---

### Fas 4: Delegation (Vecka 6)

**Mål:** Temporär access och delegation

1. **Delegation Service**
   - Skapa delegation
   - Validera delegation
   - Återkalla delegation

2. **UI**
   - Delegation dialog
   - Active delegations list
   - Notifikationer

3. **Background Jobs**
   - Auto-revoke vid slutdatum
   - Påminnelser

**Leverabler:**
- ✅ Delegation fungerar
- ✅ Auto-revoke implementerad
- ✅ Tester passed

---

### Fas 5: Audit & Monitoring (Vecka 7)

**Mål:** Fullständig audit trail

1. **Audit Service**
   - Logga alla events
   - Retention policy
   - Query interface

2. **Audit UI**
   - AuditLog.razor component
   - Filtering och search
   - Export till CSV/PDF

3. **Monitoring**
   - Dashboard för admins
   - Alert på kritiska events

**Leverabler:**
- ✅ Alla händelser loggas
- ✅ UI för att se audit log
- ✅ Export fungerar

---

### Fas 6: Polish & Documentation (Vecka 8)

**Mål:** Finslipning och dokumentation

1. **UX Improvements**
   - Tooltips och help text
   - Better error messages
   - Loading states

2. **Documentation**
   - Användarguide (svenska)
   - Admin guide
   - API documentation

3. **Performance**
   - Caching av roller
   - Query optimization
   - Load testing

**Leverabler:**
- ✅ Dokumentation klar
- ✅ Performance goals uppnådda
- ✅ UAT godkänd

---

## Testscenarier

### Testscenario 1: Grundläggande Rollhantering

**Test Case 1.1: Skapa hushåll som Admin**
```
Given: Användare "Anna" är inloggad
When: Anna skapar nytt hushåll "Familjen Andersson"
Then: Anna är automatiskt Admin
And: Audit log innehåller "HouseholdCreated" och "RoleAssigned"
```

**Test Case 1.2: Lägg till medlem med Full Access**
```
Given: Anna är Admin i "Familjen Andersson"
When: Anna lägger till "Per" som Full Access
Then: Per kan se och redigera alla transaktioner
And: Per kan INTE lägga till nya medlemmar
And: Audit log innehåller "MemberAdded" och "RoleAssigned"
```

**Test Case 1.3: Försök radera hushåll som non-Admin**
```
Given: Per har Full Access (ej Admin)
When: Per försöker radera hushållet
Then: Åtgärd blockeras med felmeddelande "Endast Admin kan radera hushåll"
And: Audit log innehåller "UnauthorizedAccess"
```

---

### Testscenario 2: Behörighetsgränser

**Test Case 2.1: Editor skapar stor transaktion**
```
Given: Lisa har Editor-roll
When: Lisa skapar transaktion på 8,000 kr
Then: Transaktion skapas som "Pending Approval"
And: Admin får notifikation om godkännande krävs
And: Transaktion syns som "Väntar på godkännande" för Lisa
```

**Test Case 2.2: View Only försöker redigera**
```
Given: Farmor har View Only-roll
When: Farmor öppnar Transactions-sidan
Then: Alla edit/delete-knappar är disabled
And: "Skapa ny transaktion"-knapp är disabled
And: Tooltip visar "Du har endast läsrättigheter"
```

**Test Case 2.3: Limited ser endast egen data**
```
Given: Erik har Limited-roll
And: Hushållet har 50 transaktioner (10 av Erik, 40 av andra)
When: Erik öppnar Transactions-sidan
Then: Erik ser endast sina egna 10 transaktioner
And: Totalsummor beräknas endast på Eriks data
```

---

### Testscenario 3: Child Role och Ålders-transitions

**Test Case 3.1: Barn under 13 år**
```
Given: Emma är 10 år och har Child-roll
When: Emma försöker logga in
Then: Login blockeras med meddelande "Barn under 13 år kan inte logga in"
And: Förälder kan fortfarande hantera Emmas barnkonto
```

**Test Case 3.2: Barn fyller 13 år**
```
Given: Emma fyller 13 år idag
When: Systemets background job körs
Then: Föräldrar får notifikation "Emma kan nu få inloggning"
And: Föräldrar kan aktivera login för Emma
And: Temporärt lösenord genereras
```

**Test Case 3.3: Barn fyller 18 år**
```
Given: Emma fyller 18 år idag
When: Systemets background job körs
Then: Emmas Child-roll inaktiveras
And: Emma får automatiskt Limited-roll
And: Emma och föräldrar får notifikation om uppgraderingen
And: Audit log innehåller "ChildAgeUpgrade"
```

---

### Testscenario 4: Delegation

**Test Case 4.1: Admin delegerar Full Access**
```
Given: Anna är Admin
When: Anna delegerar Full Access till "Sven" i 14 dagar
Then: Sven får Full Access-rättigheter omedelbart
And: Sven och Anna får notifikation
And: Delegation syns i Active Delegations-lista
And: Delegation upphör automatiskt efter 14 dagar
```

**Test Case 4.2: Full Access delegerar Editor (kräver godkännande)**
```
Given: Per har Full Access
When: Per delegerar Editor-roll till "Lisa" i 7 dagar
Then: Delegation skapas som "Pending Approval"
And: Admin får notifikation om godkännande krävs
And: Lisa får INTE Editor-rättigheter än
When: Admin godkänner delegationen
Then: Lisa får Editor-rättigheter
And: Lisa får notifikation
```

**Test Case 4.3: Delegation återkallas manuellt**
```
Given: Anna har delegerat Full Access till Sven
When: Anna återkallar delegationen efter 5 dagar
Then: Svens Full Access-rättigheter tas bort omedelbart
And: Sven får notifikation om återkallelse
And: Audit log innehåller "DelegationRevoked" med reason
```

**Test Case 4.4: Delegerad användare kan inte delegera vidare**
```
Given: Sven har delegerad Full Access från Anna
When: Sven försöker delegera Editor till någon annan
Then: Åtgärd blockeras med "Delegerade rättigheter kan inte delegeras vidare"
```

---

### Testscenario 5: Edge Cases

**Test Case 5.1: Admin lämnar hushåll**
```
Given: Anna är Admin i hushåll med 3 medlemmar
When: Anna försöker lämna hushållet
Then: System blockerar med "Du måste först överföra Admin-rollen"
When: Anna överför Admin till Per
Then: Annas Admin-roll övergår till Per
And: Anna får Limited-roll automatiskt
And: Anna kan nu lämna hushållet om hon vill
```

**Test Case 5.2: Sista medlemmen lämnar**
```
Given: Hushåll har endast 1 medlem (Admin Anna)
When: Anna lämnar hushållet
Then: System visar varning "Hushållet kommer raderas"
When: Anna bekräftar
Then: Hushållet raderas
And: Alla associerade data raderas (transaktioner, budgetar, etc.)
And: Audit log skapas innan radering
```

**Test Case 5.3: Medlem med delegerad roll lämnar**
```
Given: Anna har delegerat Editor till Lisa i 30 dagar
When: Anna lämnar hushållet efter 10 dagar
Then: Delegationen till Lisa återkallas automatiskt
And: Lisa får notifikation
And: Audit log innehåller "DelegationRevokedDueToMemberLeaving"
```

---

## Checklista för Implementation

### Development Checklist

#### Datamodell
- [ ] `HouseholdRole` tabell skapad
- [ ] `RolePermission` tabell skapad
- [ ] `AuditLogEntry` tabell skapad
- [ ] Migrationer skapade och testade
- [ ] Seed data för default permissions
- [ ] Database constraints för one-admin-per-household

#### Services
- [ ] `IRbacService` interface definierad
- [ ] `RbacService` implementerad
- [ ] `IAuditService` interface definierad
- [ ] `AuditService` implementerad
- [ ] Unit tests för alla services (>80% coverage)
- [ ] Integration tests för kritiska flöden

#### Authorization
- [ ] Custom authorization policies definierade
- [ ] `RequireHouseholdRole` attribute implementerad
- [ ] Authorization handlers implementerade
- [ ] Policy tests

#### UI Components
- [ ] `RoleManagement.razor` skapad
- [ ] `RoleAssignmentDialog.razor` skapad
- [ ] `DelegationManager.razor` skapad
- [ ] `AuditLog.razor` skapad
- [ ] Permission indicators i befintliga komponenter
- [ ] Role badges i navigation

#### Business Logic
- [ ] Rollvalidering implementerad
- [ ] Delegationslogik implementerad
- [ ] Age transition logic implementerad
- [ ] Permission check logic implementerad
- [ ] Amount limit validation implementerad

#### Background Jobs
- [ ] `CheckChildAgeTransitionsJob` implementerad
- [ ] `RevokeDelegationJob` implementerad
- [ ] `SendDelegationExpiryReminderJob` implementerad
- [ ] Job scheduling konfigurerad

#### Notifikationer
- [ ] RoleDelegated notification
- [ ] DelegationRevoked notification
- [ ] DelegationExpiring notification
- [ ] ChildAgeTransition notification
- [ ] PermissionDenied notification

#### Dokumentation
- [ ] RBAC_SPECIFICATION.md (detta dokument)
- [ ] RBAC_USER_GUIDE.md (användarguide på svenska)
- [ ] API documentation uppdaterad
- [ ] Code comments för komplex logik
- [ ] README uppdaterad

#### Testing
- [ ] Alla testscenarier i detta dokument genomförda
- [ ] Edge cases testade
- [ ] Performance testing (>1000 medlemmar i hushåll)
- [ ] Security testing (penetration test)
- [ ] UAT med riktiga användare

#### Security
- [ ] SQL injection skydd verifierat
- [ ] XSS skydd verifierat
- [ ] CSRF skydd verifierat
- [ ] Audit log är immutable
- [ ] Sensitive data encryption
- [ ] Rate limiting på API

---

## Framtida Utökningar

### Fas 2 Funktioner (Post-MVP)

1. **Finmaskig Permission Control**
   - Custom permissions per användare
   - Permission groups
   - Time-based permissions

2. **Approval Workflows**
   - Multi-step godkännande
   - Approval chains
   - Escalation rules

3. **Role Templates**
   - Fördefinierade rollmallar
   - Import/export av roller
   - Cross-household role copying

4. **Advanced Audit**
   - Real-time audit dashboard
   - Anomaly detection
   - Compliance reports (GDPR, etc.)

5. **External Integration**
   - OAuth for delegation
   - SAML SSO
   - API key management

---

## Sammanfattning

Detta dokument specificerar ett komplett RBAC-system för Privatekonomi som:

✅ **Definierar 6 distinkta roller** med tydliga behörigheter  
✅ **Hanterar delegation** med tidsbegränsningar och godkännanden  
✅ **Loggar alla ändringar** i ett komplett audit trail  
✅ **Hanterar edge cases** som åldersövergångar och medlemsbyten  
✅ **Säkerställer säkerhet** genom defense in depth  
✅ **Ger implementation guidelines** för utvecklare  

**Estimerat arbete:** 8-10 veckor för fullständig implementation  
**Prioritet:** Medel (kan delas upp i faser)  
**Beroenden:** Befintlig household- och authentication-infrastruktur

---

**Författare:** GitHub Copilot  
**Granskad av:** -  
**Godkänd av:** -  
**Nästa steg:** Skapa GitHub issue från detta dokument och börja Fas 1

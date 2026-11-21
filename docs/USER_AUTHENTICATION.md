# User Authentication and Management

> **Arkitekturstatus:** Web och API har separata Identity-lager. Web-konton
> skapas inte automatiskt i API och Web använder fortfarande sina egna
> datatjänster och lagring. API-inloggningen är begränsad till de endpoints som
> beskrivs i [API-guiden](API_GUIDE.md).

## Overview

Privatekonomi now includes a comprehensive user authentication and management system built on ASP.NET Core Identity. This enables multiple users to:

- Register and create their own accounts
- Log in securely with email and password
- Have completely isolated data (transactions, budgets, goals, etc.)
- Access the system from multiple devices simultaneously
- Optionally share household data when members of the same household

## Features

### User Registration and Login

- **Registration**: Users can create accounts with email, password, first name, and last name
- **Login**: Secure authentication with email and password
- **Password Requirements**: Minimum 12 characters
- **Remember Me**: Optional persistent login sessions
- **Logout**: CSRF-protected session termination
- **Email confirmation**: Enabled in production when SMTP and the public HTTPS URL are configured
- **Password recovery**: Email-based, with a generic response that does not disclose whether an account exists
- **Account protection**: Five failed password attempts trigger a 15-minute lockout; account endpoints are rate-limited

### Data Isolation

Each user's data is completely isolated:

- **Transactions**: Only visible to the user who created them
- **Budgets**: Each user has their own budgets
- **Goals**: Personal savings goals per user
- **Bank Sources**: Bank accounts belong to individual users
- **Investments, Assets, Loans**: User-specific financial data

### Household Sharing (Optional)

Users can be members of households to share expenses and manage joint finances:

- Multiple users can belong to the same household
- Shared expenses can be split between household members
- Different split methods: equal, by percentage, by amount, by room size

## Getting Started

### Test User Account

In Development kan ett testkonto skapas endast när demofunktionen är uttryckligen
aktiverad och e-post/lösenord har konfigurerats. Kontot skapas inte automatiskt
i andra miljöer och dessa uppgifter ska aldrig användas för riktiga konton.

Demokontot och exempeldata följer den aktuella processens lagrings- och
seedningsregler; Web och API delar inte automatiskt konto eller data. Se
[lagringsguiden](STORAGE_GUIDE.md).

### Creating a New Account

1. Navigate to the application
2. Click "Logga in" (Login) in the top navigation bar
3. Click "Registrera dig" (Register) on the login page
4. Fill in your details:
   - First name
   - Last name
   - Email address
   - Password (min 12 characters)
   - Confirm password
5. Click "Registrera" (Register)
6. In development, you'll be logged in and redirected to onboarding. When email confirmation is enabled, confirm your email before logging in.

### Logging In

1. Click "Logga in" (Login) in the navigation bar
2. Enter your email and password
3. Optionally check "Kom ihåg mig" (Remember me) for persistent sessions
4. Click "Logga in" (Login)

### Logging Out

Click "Logga ut" (Logout) in the navigation bar when logged in.

## Technical Implementation

### Architecture

The authentication system is built using:

- **ASP.NET Core Identity**: Robust authentication framework
- **Entity Framework Core**: User data storage
- **Cookie Authentication**: Secure session management
- **Storage**: Web has its own configurable Identity and database; the separate
  API process has its own configuration and does not automatically share accounts.
  See [storage guide](STORAGE_GUIDE.md).

### User Model

```csharp
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int? HouseholdMemberId { get; set; }
    public HouseholdMember? HouseholdMember { get; set; }
}
```

### Data Filtering

All services automatically filter data based on the current authenticated user:

- `TransactionService`: Only returns transactions for the current user
- `BudgetService`: Only shows budgets created by the current user
- `GoalService`: Only displays the current user's goals
- `BankSourceService`: Only lists the current user's bank accounts

This is implemented using the `ICurrentUserService` interface, which provides:
- `UserId`: The current authenticated user's ID
- `IsAuthenticated`: Whether a user is currently logged in

### Security Considerations

The application enforces a 12-character minimum password, account lockout, rate limits, CSRF checks for account forms and passkey actions, and local-only post-login redirects. Email confirmation is required automatically in production when the SMTP host and public HTTPS origin are configured. To require confirmation explicitly, set `Authentication:RequireConfirmedAccount` to `true`; startup then fails if the required email settings are missing.

Configure email delivery using environment variables or another secret provider (do not commit SMTP credentials):

```text
Email__SmtpHost=smtp.example.com
Email__SmtpPort=587
Email__SmtpUser=...
Email__SmtpPassword=...
Email__FromAddress=noreply@example.com
Email__FromName=Privatekonomi
Email__PublicBaseUrl=https://finance.example.com
Authentication__RequireConfirmedAccount=true
```

`Email:PublicBaseUrl` must be the public HTTPS origin without a path. Confirmation and password-reset links are generated only from this configured value, not from the incoming request's Host header. SMTP uses STARTTLS on the configured port.

Passkeys can be added and removed from the signed-in user's Passkeys page. WebAuthn operations require HTTPS or `localhost`; an HTTP LAN address is not a secure browser context.

### Changing storage

SQL Server is already a supported `Storage` provider. Changing provider does not
transfer accounts or other data. Back up first and consult the
[storage guide](STORAGE_GUIDE.md) for configuration and schema caveats.

## API Authentication

The alpha personal API uses its own Identity store and password login at
`POST /api/session/login` over HTTPS. The returned bearer token authorizes the
personal transaction endpoints; it is not the Web cookie. API registration
and a two-factor login flow are not provided, and accounts with two-factor
enabled are rejected. Web does not use this personal API. Legacy controllers are
blocked by default and normally return 403 even for personal tokens; earlier
versions may expose data anonymously. See [legacy API security](LEGACY_API_SECURITY.md) and the
[developer quickstart](DEVELOPER_QUICKSTART.md).

## Passkeys

Signed-in users can register a passkey from **Passkeys** in the application header. Once registered, select **Logga in med passkey** on the login page to sign in without entering an email address or password. Passkeys are created as discoverable credentials so the authenticator can identify the account during passwordless sign-in.

Passkeys require a browser with WebAuthn support and a secure origin: use HTTPS, or `localhost` during development. They are stored in the `AspNetUserPasskeys` Identity table.

## Future Enhancements

Planned features for the authentication system:

- [ ] Social login (Google, Microsoft, GitHub)
- [ ] Two-factor authentication (2FA)
- [ ] User profile management page
- [ ] Admin user roles and permissions
- [ ] Audit logging for security events
- [ ] Session management (view active sessions, logout from all devices)

## Troubleshooting

### Can't log in

- Verify your email and password are correct
- Ensure caps lock is off
- Try registering a new account if you've forgotten your password
- Check browser console for errors

### Data not showing after login

- Data is isolated per user
- If you're a new user, you won't see the test data
- Demo data is created only under the configured Development seed policy; Web and API data are separate.

### Session expired

- Check "Remember me" during login for persistent sessions
- Re-login if your session has expired

## Support

For issues or questions about authentication:
1. Check this documentation
2. Review the code in `src/Privatekonomi.Web/Components/Pages/Account/`
3. Open an issue on GitHub

## References

- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Blazor Authentication and Authorization](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/)

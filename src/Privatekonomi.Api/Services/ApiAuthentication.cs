namespace Privatekonomi.Api.Services;

/// <summary>Authentication scheme reserved for the isolated personal API.</summary>
public static class ApiAuthentication
{
    public const string Scheme = "PersonalApiBearer";
    public const string LegacyDenyScheme = "LegacyDeny";
}

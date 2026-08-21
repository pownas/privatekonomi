using Microsoft.AspNetCore.DataProtection;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Token encryption service using ASP.NET Core Data Protection API
/// </summary>
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly IDataProtector _protector;
    private const string EncryptionPrefix = "ENC:";

    public TokenEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        // Create a protector with a specific purpose string
        _protector = dataProtectionProvider.CreateProtector("Privatekonomi.BankTokens.v1");
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        // Don't double-encrypt
        if (IsEncrypted(plaintext))
            return plaintext;

        var encrypted = _protector.Protect(plaintext);
        return EncryptionPrefix + encrypted;
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;

        // If not encrypted, return as-is (for backward compatibility)
        if (!IsEncrypted(ciphertext))
            return ciphertext;

        var encrypted = ciphertext.Substring(EncryptionPrefix.Length);
        return _protector.Unprotect(encrypted);
    }

    public bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptionPrefix, StringComparison.Ordinal);
    }
}

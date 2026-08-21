using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class TokenEncryptionServiceTests
{
    private readonly TokenEncryptionService _encryptionService;

    public TokenEncryptionServiceTests()
    {
        // Setup Data Protection for testing
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        _encryptionService = new TokenEncryptionService(dataProtectionProvider);
    }

    [TestMethod]
    public void Encrypt_ShouldReturnEncryptedString()
    {
        // Arrange
        var plaintext = "my-secret-token-12345";

        // Act
        var encrypted = _encryptionService.Encrypt(plaintext);

        // Assert
        Assert.IsNotNull(encrypted);
        Assert.AreNotEqual(plaintext, encrypted);
        StringAssert.StartsWith(encrypted, "ENC:");
    }

    [TestMethod]
    public void Decrypt_ShouldReturnOriginalString()
    {
        // Arrange
        var plaintext = "my-secret-token-12345";
        var encrypted = _encryptionService.Encrypt(plaintext);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.AreEqual(plaintext, decrypted);
    }

    [TestMethod]
    public void Encrypt_ShouldNotDoubleEncrypt()
    {
        // Arrange
        var plaintext = "my-secret-token-12345";
        var encrypted = _encryptionService.Encrypt(plaintext);

        // Act
        var encryptedAgain = _encryptionService.Encrypt(encrypted);

        // Assert
        Assert.AreEqual(encrypted, encryptedAgain);
    }

    [TestMethod]
    public void Decrypt_UnencryptedString_ShouldReturnAsIs()
    {
        // Arrange
        var plaintext = "not-encrypted-token";

        // Act
        var result = _encryptionService.Decrypt(plaintext);

        // Assert
        Assert.AreEqual(plaintext, result);
    }

    [TestMethod]
    public void IsEncrypted_ShouldDetectEncryptedStrings()
    {
        // Arrange
        var plaintext = "my-secret-token-12345";
        var encrypted = _encryptionService.Encrypt(plaintext);

        // Act & Assert
        Assert.IsTrue(_encryptionService.IsEncrypted(encrypted));
        Assert.IsFalse(_encryptionService.IsEncrypted(plaintext));
    }

    [TestMethod]
    public void Encrypt_NullOrEmpty_ShouldReturnAsIs()
    {
        // Arrange & Act & Assert
        Assert.IsNull(_encryptionService.Encrypt(null!));
        Assert.AreEqual(string.Empty, _encryptionService.Encrypt(string.Empty));
    }

    [TestMethod]
    public void Decrypt_NullOrEmpty_ShouldReturnAsIs()
    {
        // Arrange & Act & Assert
        Assert.IsNull(_encryptionService.Decrypt(null!));
        Assert.AreEqual(string.Empty, _encryptionService.Decrypt(string.Empty));
    }

    [TestMethod]
    public void RoundTrip_MultipleTokens_ShouldWork()
    {
        // Arrange
        var tokens = new[]
        {
            "access-token-abc123",
            "refresh-token-xyz789",
            "another-token-with-special-chars!@#$%"
        };

        // Act & Assert
        foreach (var token in tokens)
        {
            var encrypted = _encryptionService.Encrypt(token);
            var decrypted = _encryptionService.Decrypt(encrypted);
            
            Assert.AreEqual(token, decrypted);
            Assert.AreNotEqual(token, encrypted);
            Assert.IsTrue(_encryptionService.IsEncrypted(encrypted));
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class OAuthStateServiceTests
{
    private readonly OAuthStateService _stateService;

    public OAuthStateServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _stateService = new OAuthStateService(cache);
    }

    [TestMethod]
    public void GenerateState_ShouldReturnNonEmptyString()
    {
        // Arrange
        var bankName = "Swedbank";

        // Act
        var state = _stateService.GenerateState(bankName);

        // Assert
        Assert.IsNotNull(state);
        Assert.AreNotEqual(0, state.Count());
    }

    [TestMethod]
    public void GenerateState_ShouldReturnUniqueStates()
    {
        // Arrange
        var bankName = "Swedbank";

        // Act
        var state1 = _stateService.GenerateState(bankName);
        var state2 = _stateService.GenerateState(bankName);

        // Assert
        Assert.AreNotEqual(state1, state2);
    }

    [TestMethod]
    public void ValidateState_ValidState_ShouldReturnTrue()
    {
        // Arrange
        var bankName = "Swedbank";
        var state = _stateService.GenerateState(bankName);

        // Act
        var isValid = _stateService.ValidateState(state, bankName);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void ValidateState_InvalidState_ShouldReturnFalse()
    {
        // Arrange
        var bankName = "Swedbank";
        var invalidState = "invalid-state-12345";

        // Act
        var isValid = _stateService.ValidateState(invalidState, bankName);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateState_WrongBankName_ShouldReturnFalse()
    {
        // Arrange
        var bankName = "Swedbank";
        var state = _stateService.GenerateState(bankName);

        // Act
        var isValid = _stateService.ValidateState(state, "ICA-banken");

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateState_EmptyState_ShouldReturnFalse()
    {
        // Arrange & Act
        var isValid = _stateService.ValidateState(string.Empty, "Swedbank");

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void RemoveState_ShouldInvalidateState()
    {
        // Arrange
        var bankName = "Swedbank";
        var state = _stateService.GenerateState(bankName);

        // Act
        _stateService.RemoveState(state);
        var isValid = _stateService.ValidateState(state, bankName);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void RemoveState_NonExistentState_ShouldNotThrow()
    {
        // Arrange
        var nonExistentState = "non-existent-state";

        // Act & Assert
        // Should not throw exception
        _stateService.RemoveState(nonExistentState);
    }

    [TestMethod]
    public void ValidateState_CaseInsensitiveBankName_ShouldWork()
    {
        // Arrange
        var state = _stateService.GenerateState("Swedbank");

        // Act
        var isValid1 = _stateService.ValidateState(state, "swedbank");
        var isValid2 = _stateService.ValidateState(state, "SWEDBANK");

        // Assert
        Assert.IsTrue(isValid1);
        Assert.IsTrue(isValid2);
    }

    [TestMethod]
    public void StateLifecycle_GenerateValidateRemove_ShouldWork()
    {
        // Arrange
        var bankName = "ICA-banken";

        // Act - Generate
        var state = _stateService.GenerateState(bankName);
        Assert.IsNotNull(state);

        // Act - Validate (should be valid)
        var isValidBefore = _stateService.ValidateState(state, bankName);
        Assert.IsTrue(isValidBefore);

        // Act - Remove
        _stateService.RemoveState(state);

        // Act - Validate again (should be invalid after removal)
        var isValidAfter = _stateService.ValidateState(state, bankName);
        Assert.IsFalse(isValidAfter);
    }
}

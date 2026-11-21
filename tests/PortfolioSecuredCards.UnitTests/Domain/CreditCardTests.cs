using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Domain.Exceptions;

namespace PortfolioSecuredCards.UnitTests.Domain;

public sealed class CreditCardTests
{
    [Fact]
    public void Issue_WhenCreditLimitExceedsLien_ShouldThrowDomainException()
    {
        // Act
        var exception = Assert.Throws<DomainException>(() =>
            CreateCreditCard(
                lienAmount: 10_000m,
                creditLimit: 15_000m));

        // Assert
        Assert.Equal(
            BusinessErrorCodes.CreditLimitExceedsLien,
            exception.ErrorCode);
    }

    [Fact]
    public void Use_WhenAmountExceedsCreditLimit_ShouldThrowDomainException()
    {
        // Arrange
        var card = CreateCreditCard(creditLimit: 8_000m);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => card.Use(9_000m, DateTimeOffset.UtcNow));

        // Assert
        Assert.Equal(
            BusinessErrorCodes.CreditLimitExceeded,
            exception.ErrorCode);

        Assert.Equal(0m, card.UtilizedAmount);
    }

    [Fact]
    public void MarkDelinquent_WhenThresholdReached_ShouldChangeStatus()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var card = CreateCreditCard();

        card.Use(10_000m, now);

        // Act
        var changed = card.MarkDelinquent(
            now.AddHours(25),
            TimeSpan.FromHours(24));

        // Assert
        Assert.True(changed);
        Assert.Equal(
            CreditCardStatus.Delinquent,
            card.Status);
    }

    private static CreditCard CreateCreditCard(
        decimal lienAmount = 10_000m,
        decimal creditLimit = 10_000m)
    {
        return CreditCard.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            lienAmount,
            creditLimit,
            "4111111111111111",
            DateTimeOffset.UtcNow);
    }
}
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Domain.Exceptions;

namespace PortfolioSecuredCards.UnitTests.Domain;

public sealed class PortfolioTests
{
    [Fact]
    public void CreatePortfolio_WithValidData_ShouldSucceed()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var portfolio = CreatePortfolio(customerId: customerId);

        // Assert
        Assert.NotEqual(Guid.Empty, portfolio.Id);
        Assert.Equal(customerId, portfolio.CustomerId);
        Assert.Equal(PortfolioType.Premium, portfolio.Type);
        Assert.Equal(PortfolioStatus.Active, portfolio.Status);
        Assert.Equal(50_000m, portfolio.TotalValue);
        Assert.Equal(50_000m, portfolio.AvailableValue);
        Assert.Empty(portfolio.Liens);
    }

    [Fact]
    public void ReserveLien_WhenAmountExceedsAvailableValue_ShouldThrowDomainException()
    {
        // Arrange
        var portfolio = CreatePortfolio();
        var now = DateTimeOffset.UtcNow;

        portfolio.ReserveLien(30_000m, now);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => portfolio.ReserveLien(25_000m, now));

        // Assert
        Assert.Equal(
            BusinessErrorCodes.InsufficientAvailableValue,
            exception.ErrorCode);

        Assert.Equal(30_000m, portfolio.TotalLienAmount);
        Assert.Equal(20_000m, portfolio.AvailableValue);
        Assert.Single(portfolio.Liens);
    }

    [Fact]
    public void Redeem_WhenAmountExceedsAvailableValue_ShouldThrowDomainException()
    {
        // Arrange
        var portfolio = CreatePortfolio();

        portfolio.ReserveLien(30_000m, DateTimeOffset.UtcNow);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => portfolio.Redeem(25_000m));

        // Assert
        Assert.Equal(
            BusinessErrorCodes.InsufficientAvailableValue,
            exception.ErrorCode);

        Assert.Equal(50_000m, portfolio.TotalValue);
        Assert.Equal(20_000m, portfolio.AvailableValue);
    }

    private static Portfolio CreatePortfolio(
        Guid? customerId = null,
        PortfolioType type = PortfolioType.Premium,
        decimal totalValue = 50_000m)
    {
        return new Portfolio(
            customerId ?? Guid.NewGuid(),
            type,
            totalValue);
    }
}
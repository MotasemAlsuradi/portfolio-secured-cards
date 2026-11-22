
using Moq;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Application.Configuration;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.MarkCardDelinquent;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.UnitTests.Helpers;

namespace PortfolioSecuredCards.UnitTests.Application.CreditCards;

public sealed class MarkCardDelinquentCommandHandlerTests
{
    [Fact]
    public async Task EligibleCard_ShouldBecomeDelinquent_AndFreezePortfolio()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var (card, portfolio) =
            CreditCardTestFactory.CreateFullyUtilizedCard(now);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = CreateHandler(
            card,
            portfolio,
            unitOfWork.Object);

        // Act
        var result = await handler.Handle(
            new MarkCardDelinquentCommand(card.Id, now),
            CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(CreditCardStatus.Delinquent, card.Status);
        Assert.Equal(PortfolioStatus.Frozen, portfolio.Status);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AlreadyDelinquentCard_ShouldNotBeProcessedAgain()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var (card, portfolio) =
            CreditCardTestFactory.CreateFullyUtilizedCard(now);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = CreateHandler(
            card,
            portfolio,
            unitOfWork.Object);

        var command = new MarkCardDelinquentCommand(card.Id, now);

        // Act
        var firstResult = await handler.Handle(
            command,
            CancellationToken.None);

        var secondResult = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult);

        Assert.Equal(CreditCardStatus.Delinquent, card.Status);
        Assert.Equal(PortfolioStatus.Frozen, portfolio.Status);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static MarkCardDelinquentCommandHandler CreateHandler(
        CreditCard card,
        Portfolio portfolio,
        IUnitOfWork unitOfWork)
    {
        var cardRepository = new Mock<ICreditCardRepository>();

        cardRepository
            .Setup(x => x.GetByIdAsync(
                card.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        var portfolioRepository = new Mock<IPortfolioRepository>();

        portfolioRepository
            .Setup(x => x.GetByIdAsync(
                portfolio.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        return new MarkCardDelinquentCommandHandler(
            cardRepository.Object,
            portfolioRepository.Object,
            unitOfWork,
            new DelinquencyOptions
            {
                ThresholdMinutes = 1
            });
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Application.Abstractions.Processing;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.RunDelinquencyScan;
using PortfolioSecuredCards.UnitTests.Helpers;

namespace PortfolioSecuredCards.UnitTests.Application.CreditCards;

public sealed class RunDelinquencyScanCommandHandlerTests
{
    [Fact]
    public async Task Scan_ShouldContinueProcessing_WhenOneCardFails()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var cards = Enumerable.Range(0, 3)
            .Select(_ => CreditCardTestFactory.CreateFullyUtilizedCard(now).Card)
            .ToArray();

        var cardRepository = new Mock<ICreditCardRepository>();

        cardRepository
            .Setup(x => x.GetFullyUtilizedActiveCardsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cards);

        var processor = new Mock<ICardDelinquencyProcessor>();

        processor
            .Setup(x => x.ProcessAsync(
                cards[0].Id,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        processor
            .Setup(x => x.ProcessAsync(
                cards[1].Id,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated failure"));

        processor
            .Setup(x => x.ProcessAsync(
                cards[2].Id,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RunDelinquencyScanCommandHandler(
            cardRepository.Object,
            processor.Object,
            TimeProvider.System,
            NullLogger<RunDelinquencyScanCommandHandler>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new RunDelinquencyScanCommand(),
                CancellationToken.None));

        // Assert
        Assert.Contains("1 failed card", exception.Message);

        foreach (var card in cards)
        {
            processor.Verify(
                x => x.ProcessAsync(
                    card.Id,
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
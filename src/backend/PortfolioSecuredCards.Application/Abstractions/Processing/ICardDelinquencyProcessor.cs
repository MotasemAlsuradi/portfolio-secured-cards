
namespace PortfolioSecuredCards.Application.Abstractions.Processing;

public interface ICardDelinquencyProcessor
{
    Task<bool> ProcessAsync(
        Guid cardId,
        DateTimeOffset scanTime,
        CancellationToken cancellationToken = default);
}
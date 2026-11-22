
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Application.Abstractions.Persistence;

public interface ICardUsageHistoryRepository
{
    Task AddAsync(CardUsageHistory history, CancellationToken cancellationToken = default);
}
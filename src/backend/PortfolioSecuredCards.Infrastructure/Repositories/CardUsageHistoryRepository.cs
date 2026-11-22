
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.Repositories;

public sealed class CardUsageHistoryRepository
    : ICardUsageHistoryRepository
{
    private readonly AppDbContext _context;

    public CardUsageHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CardUsageHistory history, CancellationToken cancellationToken = default)
    {
        await _context.CardUsageHistories.AddAsync(
            history, 
            cancellationToken);
    }
}
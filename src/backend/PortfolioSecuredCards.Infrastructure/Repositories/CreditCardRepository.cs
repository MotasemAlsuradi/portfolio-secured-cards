
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.Repositories;

public sealed class CreditCardRepository : ICreditCardRepository
{
    private readonly AppDbContext _context;

    public CreditCardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CreditCard?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards.FirstOrDefaultAsync(
            card => card.Id == cardId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CreditCard>> GetFullyUtilizedActiveCardsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .Where(card => 
                card.Status == CreditCardStatus.Active &&
                card.UtilizedAmount == card.CreditLimit &&
                card.FullyUtilizedSince != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOtherDelinquentCardsAsync(Guid portfolioId, Guid excludingCardId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditCards
            .AnyAsync(
                card =>
                    card.PortfolioId == portfolioId &&
                    card.Id == excludingCardId &&
                    card.Status == CreditCardStatus.Active,
                cancellationToken);
    }

    public async Task AddAsync(CreditCard card, CancellationToken cancellationToken = default)
    {
        await _context.CreditCards.AddAsync(
            card, 
            cancellationToken);
    }
}
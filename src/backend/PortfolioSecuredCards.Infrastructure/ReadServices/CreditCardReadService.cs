
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Queries;
using PortfolioSecuredCards.Application.Features.CreditCards.Queries;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.ReadServices;

public sealed class CreditCardReadService : ICreditCardReadService
{
    private readonly AppDbContext _context;

    public CreditCardReadService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CreditCardDto?> GetByIdAsync(Guid cardId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var card = await (
            from creditCard in _context.CreditCards.AsNoTracking()
            join portfolio in _context.Portfolios.AsNoTracking()
                on creditCard.PortfolioId equals portfolio.Id
            where creditCard.Id == cardId &&
                  portfolio.CustomerId == customerId
            select new
            {
                creditCard.Id,
                creditCard.PortfolioId,
                creditCard.LienId,
                creditCard.CardNumber,
                creditCard.CreditLimit,
                creditCard.UtilizedAmount,
                creditCard.Status,
                creditCard.IssuedAt,
                creditCard.FullyUtilizedSince,
                creditCard.ClosedAt
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            return null;
        }

        return new CreditCardDto(
            card.Id,
            card.PortfolioId,
            card.LienId,
            MaskCardNumber(card.CardNumber),
            card.CreditLimit,
            card.UtilizedAmount,
            card.CreditLimit - card.UtilizedAmount,
            card.Status,
            card.IssuedAt,
            card.FullyUtilizedSince,
            card.ClosedAt);
    }

    public async Task<IReadOnlyList<CreditCardDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var cards = await (
            from card in _context.CreditCards.AsNoTracking()
            join portfolio in _context.Portfolios.AsNoTracking()
                on card.PortfolioId equals portfolio.Id
            where portfolio.CustomerId == customerId
            orderby card.IssuedAt descending
            select new
            {
                card.Id,
                card.PortfolioId,
                card.LienId,
                card.CardNumber,
                card.CreditLimit,
                card.UtilizedAmount,
                card.Status,
                card.IssuedAt,
                card.FullyUtilizedSince,
                card.ClosedAt
            }
        ).ToListAsync(cancellationToken);

        return cards.Select(card => new CreditCardDto(
            card.Id,
            card.PortfolioId,
            card.LienId,
            MaskCardNumber(card.CardNumber),
            card.CreditLimit,
            card.UtilizedAmount,
            card.CreditLimit - card.UtilizedAmount,
            card.Status,
            card.IssuedAt,
            card.FullyUtilizedSince,
            card.ClosedAt
        )).ToList();
    }

    
    public async Task<IReadOnlyList<CreditCardUsageDto>> GetUsageHistoryAsync(Guid cardId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await (
            from card in _context.CreditCards.AsNoTracking()
            join portfolio in _context.Portfolios.AsNoTracking()
                on card.PortfolioId equals portfolio.Id
            where card.Id == cardId &&
                  portfolio.CustomerId == customerId
            select card.Id
        ).AnyAsync(cancellationToken);

        if (!hasAccess)
        {
            throw new KeyNotFoundException("Credit card was not found.");
        }

        return await _context.CardUsageHistories
            .AsNoTracking()
            .Where(history => history.CardId == cardId)
            .OrderByDescending(history => history.CreatedAt)
            .Select(history => new CreditCardUsageDto(
                history.Id,
                history.CardId,
                history.Amount,
                history.UtilizedBefore,
                history.UtilizedAfter,
                history.CreatedAt))
            .ToListAsync(cancellationToken);
    }


    private static string MaskCardNumber(string cardNumber)
    {
        if (cardNumber.Length <= 4)
        {
            return "****";
        }

        return $"****{cardNumber[^4..]}";
    }
}

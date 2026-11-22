
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Application.Abstractions.Persistence;

public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreditCard>> GetFullyUtilizedActiveCardsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> HasOtherDelinquentCardsAsync(
        Guid portfolioId,
        Guid excludingCardId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CreditCard card,
        CancellationToken cancellationToken = default);
}
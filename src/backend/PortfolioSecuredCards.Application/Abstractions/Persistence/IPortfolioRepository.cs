
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Application.Abstractions.Persistence;

public interface IPortfolioRepository
{
    Task<Portfolio?> GetByIdAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Portfolio portfolio,
        CancellationToken cancellationToken = default);
}
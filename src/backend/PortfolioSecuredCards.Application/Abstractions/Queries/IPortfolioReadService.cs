
using PortfolioSecuredCards.Application.Features.Portfolios.Queries.GetCustomerPortfolios;

namespace PortfolioSecuredCards.Application.Abstractions.Queries;

public interface IPortfolioReadService
{
    Task<IReadOnlyList<PortfolioDto>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
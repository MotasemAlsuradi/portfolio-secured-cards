
using PortfolioSecuredCards.Application.Features.Customers.Queries.GetCustomers;

namespace PortfolioSecuredCards.Application.Abstractions.Queries;

public interface ICustomerReadService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
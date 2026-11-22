
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}
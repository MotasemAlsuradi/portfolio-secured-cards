
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Queries;
using PortfolioSecuredCards.Application.Features.Customers.Queries.GetCustomers;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.ReadServices;

public sealed class CustomerReadService : ICustomerReadService
{
    private readonly AppDbContext _context;

    public CustomerReadService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.FullName)
            .Select(customer => new CustomerDto(
                customer.Id,
                customer.FullName))
            .ToListAsync(cancellationToken);
    }
}
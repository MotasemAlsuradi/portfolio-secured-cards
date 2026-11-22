
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(
            customer => customer.Id == customerId,
            cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(
            customer, 
            cancellationToken);
    }
}
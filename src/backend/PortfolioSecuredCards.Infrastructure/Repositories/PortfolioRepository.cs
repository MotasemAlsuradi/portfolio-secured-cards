
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.Repositories;

public sealed class PortfolioRepository : IPortfolioRepository
{
    private readonly AppDbContext _context;

    public PortfolioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Portfolio?> GetByIdAsync(Guid portfolioId, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .Include(portfolio => portfolio.Liens)
            .FirstOrDefaultAsync(
                portfolio => portfolio.Id == portfolioId,
                cancellationToken);
    }

    public async Task AddAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        await _context.Portfolios.AddAsync(
            portfolio, 
            cancellationToken);
    }
}
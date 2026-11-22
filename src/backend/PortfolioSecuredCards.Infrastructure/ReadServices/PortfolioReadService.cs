
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Queries;
using PortfolioSecuredCards.Application.Features.Portfolios.Queries.GetCustomerPortfolios;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Infrastructure.Persistence;

namespace PortfolioSecuredCards.Infrastructure.ReadServices;

public sealed class PortfolioReadService : IPortfolioReadService
{
    private readonly AppDbContext _context;

    public PortfolioReadService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PortfolioDto>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .AsNoTracking()
            .Where(portfolio => portfolio.CustomerId == customerId)
            .OrderBy(portfolio => portfolio.Type)
            .Select(portfolio => new PortfolioDto(
                portfolio.Id,
                portfolio.CustomerId,
                portfolio.Type,
                portfolio.Status,
                portfolio.TotalValue,
                portfolio.Liens
                    .Where(lien => lien.Status == LienStatus.Unreleased)
                    .Sum(lien => lien.Amount),
                portfolio.TotalValue -
                portfolio.Liens
                    .Where(lien => lien.Status == LienStatus.Unreleased)
                    .Sum(lien => lien.Amount)))
            .ToListAsync(cancellationToken);
    }
}
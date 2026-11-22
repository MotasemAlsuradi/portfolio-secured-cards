
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Queries;

namespace PortfolioSecuredCards.Application.Features.Portfolios.Queries.GetCustomerPortfolios;

public sealed class GetCustomerPortfoliosQueryHandler
    : IRequestHandler<GetCustomerPortfoliosQuery, IReadOnlyList<PortfolioDto>>
{
    private readonly IPortfolioReadService _portfolioReadService;

    public GetCustomerPortfoliosQueryHandler(IPortfolioReadService portfolioReadService)
    {
        _portfolioReadService = portfolioReadService;
    }

    public async Task<IReadOnlyList<PortfolioDto>> Handle(
        GetCustomerPortfoliosQuery request,
        CancellationToken cancellationToken)
    {
        return await _portfolioReadService.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
    }
}

using MediatR;

namespace PortfolioSecuredCards.Application.Features.Portfolios.Queries.GetCustomerPortfolios;

public sealed record GetCustomerPortfoliosQuery(Guid CustomerId)
    : IRequest<IReadOnlyList<PortfolioDto>>;
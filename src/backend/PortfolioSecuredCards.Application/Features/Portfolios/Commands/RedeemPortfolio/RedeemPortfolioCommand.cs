
using MediatR;

namespace PortfolioSecuredCards.Application.Features.Portfolios.Commands.RedeemPortfolio;

public sealed record RedeemPortfolioCommand(
    Guid PortfolioId,
    decimal Amount) : IRequest<decimal>;

using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.IssueCreditCard;

public sealed record IssueCreditCardCommand(
    Guid PortfolioId,
    decimal LienAmount,
    decimal CreditLimit) : IRequest<Guid>;
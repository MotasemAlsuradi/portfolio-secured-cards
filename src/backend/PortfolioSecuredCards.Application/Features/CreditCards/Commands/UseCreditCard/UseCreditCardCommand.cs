
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.UseCreditCard;

public sealed record UseCreditCardCommand(Guid CardId, decimal Amount) : IRequest<decimal>;
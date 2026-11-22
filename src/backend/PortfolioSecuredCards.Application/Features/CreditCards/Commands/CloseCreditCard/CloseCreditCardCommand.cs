
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.CloseCreditCard;

public sealed record CloseCreditCardCommand(Guid CardId) : IRequest;
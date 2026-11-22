
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.RepayCreditCard;

public sealed record RepayCreditCardCommand(Guid CardId, decimal Amount) : IRequest<decimal>;

using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCreditCardById;

public sealed record GetCreditCardByIdQuery(Guid CardId) : IRequest<CreditCardDto>;
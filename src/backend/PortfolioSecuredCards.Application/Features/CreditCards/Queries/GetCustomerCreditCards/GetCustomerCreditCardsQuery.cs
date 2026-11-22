
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCustomerCreditCards;

public sealed record GetCustomerCreditCardsQuery : IRequest<IReadOnlyList<CreditCardDto>>;
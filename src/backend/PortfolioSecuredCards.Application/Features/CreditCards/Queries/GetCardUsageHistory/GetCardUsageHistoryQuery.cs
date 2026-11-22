
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCardUsageHistory;

public sealed record GetCardUsageHistoryQuery(Guid CardId) : IRequest<IReadOnlyList<CreditCardUsageDto>>;
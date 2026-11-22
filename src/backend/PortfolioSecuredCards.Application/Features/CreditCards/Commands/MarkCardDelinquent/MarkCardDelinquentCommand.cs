
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.MarkCardDelinquent;

public sealed record MarkCardDelinquentCommand(Guid CardId, DateTimeOffset ScanTime) : IRequest<bool>;
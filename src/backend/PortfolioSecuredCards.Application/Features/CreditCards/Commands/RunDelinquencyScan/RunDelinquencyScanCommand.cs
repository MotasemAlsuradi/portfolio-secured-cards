
using MediatR;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.RunDelinquencyScan;

public sealed record RunDelinquencyScanCommand : IRequest<int>;
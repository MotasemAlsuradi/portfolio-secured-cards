
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Queries;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCardUsageHistory;

public sealed class GetCardUsageHistoryQueryHandler : IRequestHandler<GetCardUsageHistoryQuery, IReadOnlyList<CreditCardUsageDto>>
{
    private readonly ICreditCardReadService _creditCardReadService;
    private readonly ICurrentCustomer _currentCustomer;

    public GetCardUsageHistoryQueryHandler(ICreditCardReadService creditCardReadService, ICurrentCustomer currentCustomer)
    {
        _creditCardReadService = creditCardReadService;
        _currentCustomer = currentCustomer;
    }

    public async Task<IReadOnlyList<CreditCardUsageDto>> Handle(GetCardUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _creditCardReadService.GetUsageHistoryAsync(
            request.CardId,
            _currentCustomer.CustomerId,
            cancellationToken);
    }
}
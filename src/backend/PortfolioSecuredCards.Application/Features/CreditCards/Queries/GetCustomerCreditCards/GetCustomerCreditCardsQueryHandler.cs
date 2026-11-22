
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Queries;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCustomerCreditCards;

public sealed class GetCustomerCreditCardsQueryHandler : IRequestHandler<GetCustomerCreditCardsQuery, IReadOnlyList<CreditCardDto>>
{
    private readonly ICreditCardReadService _creditCardReadService;
    private readonly ICurrentCustomer _currentCustomer;

    public GetCustomerCreditCardsQueryHandler(ICreditCardReadService creditCardReadService, ICurrentCustomer currentCustomer)
    {
        _creditCardReadService = creditCardReadService;
        _currentCustomer = currentCustomer;
    }

    public async Task<IReadOnlyList<CreditCardDto>> Handle(GetCustomerCreditCardsQuery request, CancellationToken cancellationToken)
    {
        return await _creditCardReadService.GetByCustomerIdAsync(_currentCustomer.CustomerId, cancellationToken);
    }
}
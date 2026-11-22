
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Queries;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCreditCardById;

public sealed class GetCreditCardByIdQueryHandler : IRequestHandler<GetCreditCardByIdQuery, CreditCardDto>
{
    private readonly ICreditCardReadService _creditCardReadService;
    private readonly ICurrentCustomer _currentCustomer;

    public GetCreditCardByIdQueryHandler(ICreditCardReadService creditCardReadService, ICurrentCustomer currentCustomer)
    {
        _creditCardReadService = creditCardReadService;
        _currentCustomer = currentCustomer;
    }

    public async Task<CreditCardDto> Handle(GetCreditCardByIdQuery request, CancellationToken cancellationToken)
    {
        var card = await _creditCardReadService.GetByIdAsync(
            request.CardId,
            _currentCustomer.CustomerId,
            cancellationToken);

        if (card is null)
        {
            throw new KeyNotFoundException("Credit card was not found.");
        }

        return card;
    }
}

using PortfolioSecuredCards.Application.Features.CreditCards.Queries;

namespace PortfolioSecuredCards.Application.Abstractions.Queries;

public interface ICreditCardReadService
{
    Task<CreditCardDto?> GetByIdAsync(Guid cardId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditCardDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);


    Task<IReadOnlyList<CreditCardUsageDto>> GetUsageHistoryAsync(Guid cardId, Guid customerId, CancellationToken cancellationToken = default);
}
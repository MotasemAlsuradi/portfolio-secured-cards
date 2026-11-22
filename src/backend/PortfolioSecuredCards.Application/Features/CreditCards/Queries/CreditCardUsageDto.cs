
namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries;

public sealed record CreditCardUsageDto(
    Guid Id,
    Guid CardId,
    decimal Amount,
    decimal UtilizedBefore,
    decimal UtilizedAfter,
    DateTimeOffset CreatedAt);
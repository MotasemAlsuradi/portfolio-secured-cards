
using PortfolioSecuredCards.Domain.Enums;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Queries;

public sealed record CreditCardDto(
    Guid Id,
    Guid PortfolioId,
    Guid LienId,
    string MaskedCardNumber,
    decimal CreditLimit,
    decimal UtilizedAmount,
    decimal AvailableCredit,
    CreditCardStatus Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? FullyUtilizedSince,
    DateTimeOffset? ClosedAt);
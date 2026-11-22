
using PortfolioSecuredCards.Domain.Enums;

namespace PortfolioSecuredCards.Application.Features.Portfolios.Queries.GetCustomerPortfolios;

public sealed record PortfolioDto(
    Guid Id,
    Guid CustomerId,
    PortfolioType Type,
    PortfolioStatus Status,
    decimal TotalValue,
    decimal TotalLienAmount,
    decimal AvailableValue);
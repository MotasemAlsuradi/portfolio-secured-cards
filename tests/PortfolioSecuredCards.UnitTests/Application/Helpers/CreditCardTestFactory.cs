
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;

namespace PortfolioSecuredCards.UnitTests.Helpers;

internal static class CreditCardTestFactory
{
    public static (CreditCard Card, Portfolio Portfolio) CreateFullyUtilizedCard(DateTimeOffset now)
    {
        var portfolio = new Portfolio(
            Guid.NewGuid(),
            PortfolioType.Basic,
            10_000m);

        var issuedAt = now.AddHours(-2);

        var lien = portfolio.ReserveLien(
            5_000m,
            issuedAt);

        var card = CreditCard.Issue(
            portfolio.Id,
            lien.Id,
            lien.Amount,
            5_000m,
            Guid.NewGuid().ToString("N")[..30],
            issuedAt);

        card.Use(5_000m, issuedAt);

        return (card, portfolio);
    }
}
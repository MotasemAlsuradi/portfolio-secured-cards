
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;

namespace PortfolioSecuredCards.Infrastructure.Persistence;

public static class AppDbSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (await context.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        var customers = new[]
        {
            new Customer(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Ahmad Ali"),

            new Customer(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Omar Khaled"),

            new Customer(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Sara Hassan")
        };

        await context.Customers.AddRangeAsync(
            customers,
            cancellationToken);

        foreach (var customer in customers)
        {
            var portfolios = new[]
            {
                new Portfolio(
                    customer.Id,
                    PortfolioType.Basic,
                    10_000m),

                new Portfolio(
                    customer.Id,
                    PortfolioType.Plus,
                    25_000m),

                new Portfolio(
                    customer.Id,
                    PortfolioType.Premium,
                    50_000m)
            };

            await context.Portfolios.AddRangeAsync(
                portfolios,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
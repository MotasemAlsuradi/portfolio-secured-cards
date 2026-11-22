
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Application.Abstractions.Queries;
using PortfolioSecuredCards.Infrastructure.Persistence;
using PortfolioSecuredCards.Infrastructure.ReadServices;
using PortfolioSecuredCards.Infrastructure.Repositories;

namespace PortfolioSecuredCards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("PortfolioSecuredCardsDb"));

        services.AddScoped<IUnitOfWork>(
            provider => provider.GetRequiredService<AppDbContext>());

        // Command Side Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ICardUsageHistoryRepository, CardUsageHistoryRepository>();

        // Query Side Read Services
        services.AddScoped<ICustomerReadService, CustomerReadService>();
        services.AddScoped<IPortfolioReadService, PortfolioReadService>();
        services.AddScoped<ICreditCardReadService, CreditCardReadService>();

        // System Clock
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
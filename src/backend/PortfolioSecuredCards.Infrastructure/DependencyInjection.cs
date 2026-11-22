
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Infrastructure.Persistence;
using PortfolioSecuredCards.Infrastructure.Repositories;

namespace PortfolioSecuredCards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("PortfolioSecuredCardsDb"));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();

        return services;
    }
}
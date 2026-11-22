
using Microsoft.EntityFrameworkCore;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Lien> Liens => Set<Lien>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}


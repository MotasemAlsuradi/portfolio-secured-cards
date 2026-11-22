
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Infrastructure.Persistence.Configurations;

public sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.HasKey(portfolio => portfolio.Id);

        builder.Property(portfolio => portfolio.Type)
            .IsRequired();

        builder.Property(portfolio => portfolio.Status)
            .IsRequired();

        builder.Property(portfolio => portfolio.TotalValue)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(portfolio => portfolio.CustomerId)
            .IsRequired();

        builder.HasMany(portfolio => portfolio.Liens)
            .WithOne()
            .HasForeignKey(lien => lien.PortfolioId)
            .IsRequired();

        builder.Navigation(portfolio => portfolio.Liens)
            .HasField("_liens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(portfolio => portfolio.TotalLienAmount);
        builder.Ignore(portfolio => portfolio.AvailableValue);
    }
}
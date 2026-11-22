
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Infrastructure.Persistence.Configurations;

public sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CardNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(x => x.CardNumber)
            .IsUnique();

        builder.Property(x => x.CreditLimit)
            .IsRequired();

        builder.Property(x => x.UtilizedAmount)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .IsRequired();

        builder.Ignore(x => x.AvailableCredit);
        builder.Ignore(x => x.IsFullyUtilized);
    }
}
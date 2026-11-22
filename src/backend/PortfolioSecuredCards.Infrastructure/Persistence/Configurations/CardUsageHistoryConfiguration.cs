
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Infrastructure.Persistence.Configurations;

public sealed class CardUsageHistoryConfiguration : IEntityTypeConfiguration<CardUsageHistory>
{
    public void Configure(EntityTypeBuilder<CardUsageHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.CardId)
            .IsRequired();

        builder.Property(history => history.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(history => history.UtilizedBefore)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(history => history.UtilizedAfter)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(history => history.CreatedAt)
            .IsRequired();

        builder.HasIndex(history => history.CardId);
    }
}
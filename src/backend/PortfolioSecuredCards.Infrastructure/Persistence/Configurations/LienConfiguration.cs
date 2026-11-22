
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Infrastructure.Persistence.Configurations;

public sealed class LienConfiguration : IEntityTypeConfiguration<Lien>
{
    public void Configure(EntityTypeBuilder<Lien> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
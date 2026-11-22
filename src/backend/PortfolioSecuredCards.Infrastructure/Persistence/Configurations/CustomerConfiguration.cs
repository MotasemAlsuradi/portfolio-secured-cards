
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer  => customer.Id);

        builder.Property(customer => customer.FullName)
            .IsRequired()
            .HasMaxLength(200);
    }
}
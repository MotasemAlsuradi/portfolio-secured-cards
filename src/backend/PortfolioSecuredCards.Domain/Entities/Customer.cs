
namespace PortfolioSecuredCards.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    private Customer(){ }

    public Customer(Guid id, string fullName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Customer name is required.", nameof(fullName));

        Id = id;
        FullName = fullName.Trim();
    }
}

using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.UnitTests.Domain;

public sealed class CustomerTests
{
    [Fact]
    public void CreateCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        const string customerName = "Ahmad Ali";

        // Act
        var customer = new Customer(customerId, customerName);

        // Assert
        Assert.Equal(customerId, customer.Id);
        Assert.Equal(customerName, customer.FullName);
    }
}

using PortfolioSecuredCards.Domain.Validation;

namespace PortfolioSecuredCards.Domain.Entities;

public sealed class CardUsageHistory
{
    public Guid Id { get; private set; }
    public Guid CardId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal UtilizedBefore { get; private set; }
    public decimal UtilizedAfter { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CardUsageHistory() { }

    public CardUsageHistory(Guid cardId, decimal amount, decimal utilizedBefore, decimal utilizedAfter, DateTimeOffset createdAt)
    {
        if (cardId == Guid.Empty)
        {
            throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
        }

        MonetaryAmountValidator.ValidatePositive(amount);
        MonetaryAmountValidator.ValidateNonNegative(utilizedBefore);
        MonetaryAmountValidator.ValidateNonNegative(utilizedAfter);

        if (utilizedAfter != utilizedBefore + amount)
        {
            throw new ArgumentException("UtilizedAfter must equal UtilizedBefore plus Amount.");
        }

        Id = Guid.NewGuid();
        CardId = cardId;
        Amount = amount;
        UtilizedBefore = utilizedBefore;
        UtilizedAfter = utilizedAfter;
        CreatedAt = createdAt;
    }
}
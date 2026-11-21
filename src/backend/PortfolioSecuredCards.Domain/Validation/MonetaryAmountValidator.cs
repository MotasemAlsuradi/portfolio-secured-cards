
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Exceptions;

namespace PortfolioSecuredCards.Domain.Validation;

public static class MonetaryAmountValidator
{
    public static void ValidatePositive(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException(
                BusinessErrorCodes.InvalidAmount,
                "Amount must be greater than zero.");
        }

        ValidatePrecision(amount);
    }

    public static void ValidateNonNegative(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException(
                BusinessErrorCodes.InvalidAmount,
                "Amount cannot be negative.");
        }

        ValidatePrecision(amount);
    }

    private static void ValidatePrecision(decimal amount)
    {
        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                BusinessErrorCodes.InvalidAmount,
                "Amount cannot have more than two decimal places.");
        }
    }
}
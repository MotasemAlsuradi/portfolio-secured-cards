
namespace PortfolioSecuredCards.Domain.Constants;

public static class BusinessErrorCodes
{
    public const string InvalidAmount = nameof(InvalidAmount);

    public const string InsufficientAvailableValue = nameof(InsufficientAvailableValue);

    public const string PortfolioFrozen = nameof(PortfolioFrozen);

    public const string CreditLimitExceedsLien = nameof(CreditLimitExceedsLien);

    public const string CreditLimitExceeded = nameof(CreditLimitExceeded);

    public const string CardNotActive = nameof(CardNotActive);

    public const string OutstandingBalance = nameof(OutstandingBalance);

    public const string CardAlreadyClosed = nameof(CardAlreadyClosed);

    public const string LienAlreadyReleased = nameof(LienAlreadyReleased);

    public const string LienNotFound = nameof(LienNotFound);
}
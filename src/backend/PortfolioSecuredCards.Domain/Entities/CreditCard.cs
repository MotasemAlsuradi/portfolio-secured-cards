
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Domain.Exceptions;
using PortfolioSecuredCards.Domain.Validation;

namespace PortfolioSecuredCards.Domain.Entities;

public sealed class CreditCard
{
    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Guid LienId { get; private set; }
    public string CardNumber { get; private set; } = string.Empty;
    public decimal CreditLimit { get; private set; }
    public decimal UtilizedAmount { get; private set; }
    public CreditCardStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? FullyUtilizedSince { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public decimal AvailableCredit => CreditLimit - UtilizedAmount;
    public bool IsFullyUtilized => UtilizedAmount == CreditLimit;


    private CreditCard() { }

    private CreditCard(Guid portfolioId, Guid lienId, decimal creditLimit, string cardNumber, DateTimeOffset now)
    {
        Id = Guid.NewGuid();

        PortfolioId = portfolioId;
        LienId = lienId;
        CreditLimit = creditLimit;
        CardNumber = cardNumber;

        UtilizedAmount = 0m;
        Status = CreditCardStatus.Active;

        IssuedAt = now;
        FullyUtilizedSince = null;
        ClosedAt = null;
    }

    // Factory Method is used because issuing a credit card is a specific business operation.
    // It encapsulates issuance validation and ensures the card is created in a valid initial state.
    // The private constructor prevents bypassing these rules.
    public static CreditCard Issue(
        Guid portfolioId, Guid lienId, decimal lienAmount, 
        decimal requestedCreditLimit, string cardNumber, DateTimeOffset now)
    {
        if (portfolioId == Guid.Empty)
            throw new ArgumentException("Portfolio ID cannot be empty.", nameof(portfolioId));

        if (lienId == Guid.Empty)
            throw new ArgumentException("Lien ID cannot be empty.", nameof(lienId));

        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ArgumentException("Card number is required.", nameof(cardNumber));

        MonetaryAmountValidator.ValidatePositive(lienAmount);
        MonetaryAmountValidator.ValidatePositive(requestedCreditLimit);

        if (requestedCreditLimit > lienAmount)
        {
            throw new DomainException(
                BusinessErrorCodes.CreditLimitExceedsLien,
                "Credit limit cannot exceed the lien amount.");
        }

        return new CreditCard(portfolioId, lienId, requestedCreditLimit, cardNumber.Trim(), now);
    }


    public void Use(decimal amount, DateTimeOffset now)
    {
        if (Status != CreditCardStatus.Active)
        {
            throw new DomainException(
                BusinessErrorCodes.CardNotActive,
                "Only an active credit card can be used.");
        }

        MonetaryAmountValidator.ValidatePositive(amount);

        if (amount > AvailableCredit)
        {
            throw new DomainException(
                BusinessErrorCodes.CreditLimitExceeded,
                "The transaction exceeds the available credit limit.");
        }

        UtilizedAmount += amount;

        if (IsFullyUtilized && FullyUtilizedSince is null)
        {
            FullyUtilizedSince = now;
        }
    }

    public void Repay(decimal amount)
    {
        if (Status == CreditCardStatus.Closed)
        {
            throw new DomainException(
                BusinessErrorCodes.CardNotActive,
                "A closed credit card cannot receive repayments.");
        }

        MonetaryAmountValidator.ValidatePositive(amount);

        if (amount > UtilizedAmount)
        {
            throw new DomainException(
                BusinessErrorCodes.InvalidAmount,
                "Repayment cannot exceed the utilized amount.");
        }

        UtilizedAmount -= amount;

        if (!IsFullyUtilized)
        {
            FullyUtilizedSince = null;
        }
    }

    public bool MarkDelinquent(DateTimeOffset now, TimeSpan threshold)
    {
        if (threshold <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold), 
                "Delinquency threshold must be positive.");
        }

        if (Status != CreditCardStatus.Active)
            return false;

        if (!IsFullyUtilized || FullyUtilizedSince is null)
        {
            return false;
        }

        TimeSpan elapsed = now - FullyUtilizedSince.Value;

        if (elapsed < threshold)
        {
            return false;
        }

        Status = CreditCardStatus.Delinquent;

        return true;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status == CreditCardStatus.Closed)
        {
            throw new DomainException(
                BusinessErrorCodes.CardAlreadyClosed,
                "This credit card is already closed.");
        }

        if (UtilizedAmount != 0m)
        {
            throw new DomainException(
                BusinessErrorCodes.OutstandingBalance,
                "The credit card cannot be closed while it has an outstanding balance.");
        }

        Status = CreditCardStatus.Closed;
        ClosedAt = now;
        FullyUtilizedSince = null;
    }
}
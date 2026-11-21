
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Domain.Exceptions;
using PortfolioSecuredCards.Domain.Validation;

namespace PortfolioSecuredCards.Domain.Entities;

public sealed class Portfolio
{
    private readonly List<Lien> _liens = new();
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public PortfolioType Type { get; private set; }
    public PortfolioStatus Status { get; private set; }
    public decimal TotalValue { get; private set; }
    public IReadOnlyCollection<Lien> Liens => _liens.AsReadOnly();

    public decimal TotalLienAmount =>
        _liens
            .Where(lien => lien.Status == LienStatus.Unreleased)
            .Sum(lien => lien.Amount);

    public decimal AvailableValue => TotalValue - TotalLienAmount;

    private Portfolio(){ }

    public Portfolio(Guid customerId, PortfolioType portfolioType, decimal totalValue)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer ID cannot be empty.", nameof(customerId));
        }
        
        if (!Enum.IsDefined(portfolioType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(portfolioType), "Invalid portfolio type.");
        }

        MonetaryAmountValidator.ValidateNonNegative(totalValue);

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Type = portfolioType;
        TotalValue = totalValue;
        Status = PortfolioStatus.Active;
    }

    public Lien ReserveLien(decimal amount, DateTimeOffset now)
    {
        EnsureActive();

        MonetaryAmountValidator.ValidatePositive(amount);

        if (amount > AvailableValue)
        {
            throw new DomainException(
                BusinessErrorCodes.InsufficientAvailableValue,
                "The requested lien exceeds the available portfolio value.");
        }

        var lien = new Lien(Id, amount, now);

        _liens.Add(lien);

        return lien;
    }

    public void Redeem(decimal amount)
    {
        EnsureActive();

        MonetaryAmountValidator.ValidatePositive(amount);

        if (amount > AvailableValue)
        {
            throw new DomainException(
                BusinessErrorCodes.InsufficientAvailableValue,
                "The redemption amount exceeds the available portfolio value.");
        }

        TotalValue -= amount;
    }

    public void ReleaseLien(Guid lienId, DateTimeOffset now)
    {
        var lien = _liens.FirstOrDefault(l => l.Id == lienId);

        if (lien is null)
        {
            throw new DomainException(
                BusinessErrorCodes.LienNotFound, 
                "The specified lien does not belong to this portfolio.");
        }
        
        lien.Release(now);
    }

    public void Freeze()
    {
        if (Status == PortfolioStatus.Frozen)
            return;

        Status = PortfolioStatus.Frozen;
    }

    public void UnfreezeAfterDelinquencyResolution()
    {
        if (Status == PortfolioStatus.Active)
            return;

        Status = PortfolioStatus.Active;
    }

    private void EnsureActive()
    {
        if (Status != PortfolioStatus.Active)
        {
            throw new DomainException(
                BusinessErrorCodes.PortfolioFrozen, "The portfolio is frozen.");
        }
    }
}
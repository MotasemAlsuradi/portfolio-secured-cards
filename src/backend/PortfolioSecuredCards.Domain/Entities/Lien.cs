
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Domain.Exceptions;
using PortfolioSecuredCards.Domain.Validation;

namespace PortfolioSecuredCards.Domain.Entities;

public sealed class Lien
{
    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public decimal Amount { get; private set; }
    public LienStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    
    private Lien(){ }

    internal Lien(Guid portfolioId, decimal amount, DateTimeOffset now)
    {
        if (portfolioId == Guid.Empty)
            throw new ArgumentException("Portfolio ID cannot be empty.", nameof(portfolioId));

        MonetaryAmountValidator.ValidatePositive(amount);

        Id = Guid.NewGuid();
        PortfolioId = portfolioId;
        Amount = amount;
        Status = LienStatus.Unreleased;
        CreatedAt = now;
    }

    internal void Release(DateTimeOffset now)
    {
        if (Status == LienStatus.Released)
        {
            throw new DomainException(
                BusinessErrorCodes.LienAlreadyReleased, 
                "This lien has already been released.");
        }

        Status = LienStatus.Released;
        ReleasedAt = now;
    }
}

using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Entities;
using PortfolioSecuredCards.Domain.Enums;
using PortfolioSecuredCards.Domain.Exceptions;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.UseCreditCard;

public sealed class UseCreditCardCommandHandler : IRequestHandler<UseCreditCardCommand, decimal>
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ICardUsageHistoryRepository _usageHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentCustomer _currentCustomer;
    private readonly TimeProvider _timeProvider;

    public UseCreditCardCommandHandler(ICreditCardRepository creditCardRepository, IPortfolioRepository portfolioRepository, 
        ICardUsageHistoryRepository usageHistoryRepository, IUnitOfWork unitOfWork, ICurrentCustomer currentCustomer, TimeProvider timeProvider)
    {
        _creditCardRepository = creditCardRepository;
        _portfolioRepository = portfolioRepository;
        _usageHistoryRepository = usageHistoryRepository;
        _unitOfWork = unitOfWork;
        _currentCustomer = currentCustomer;
        _timeProvider = timeProvider;
    }

    public async Task<decimal> Handle(UseCreditCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _creditCardRepository.GetByIdAsync(
            request.CardId,
            cancellationToken);

        if (card is null)
        {
            throw new KeyNotFoundException("Credit card was not found.");
        }

        var portfolio = await _portfolioRepository.GetByIdAsync(
            card.PortfolioId,
            cancellationToken);

        if (portfolio is null ||
            portfolio.CustomerId != _currentCustomer.CustomerId)
        {
            throw new KeyNotFoundException("Credit card was not found.");
        }

        if (portfolio.Status == PortfolioStatus.Frozen)
        {
            throw new DomainException(
                BusinessErrorCodes.PortfolioFrozen,
                "Credit card usage is not allowed while the portfolio is frozen.");
        }

        var now = _timeProvider.GetUtcNow();
        var utilizedBefore = card.UtilizedAmount;

        card.Use(request.Amount, now);

        var history = new CardUsageHistory(
            card.Id,
            request.Amount,
            utilizedBefore,
            card.UtilizedAmount,
            now);

        await _usageHistoryRepository.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return card.AvailableCredit;
    }
}
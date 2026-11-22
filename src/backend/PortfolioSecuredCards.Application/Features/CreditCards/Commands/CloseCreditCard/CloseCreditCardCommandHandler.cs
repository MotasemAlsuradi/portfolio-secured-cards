
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Enums;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.CloseCreditCard;

public sealed class CloseCreditCardCommandHandler : IRequestHandler<CloseCreditCardCommand>
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentCustomer _currentCustomer;
    private readonly TimeProvider _timeProvider;

    public CloseCreditCardCommandHandler(ICreditCardRepository creditCardRepository, IPortfolioRepository portfolioRepository, IUnitOfWork unitOfWork,
        ICurrentCustomer currentCustomer, TimeProvider timeProvider)
    {
        _creditCardRepository = creditCardRepository;
        _portfolioRepository = portfolioRepository;
        _unitOfWork = unitOfWork;
        _currentCustomer = currentCustomer;
        _timeProvider = timeProvider;
    }

    public async Task Handle(CloseCreditCardCommand request, CancellationToken cancellationToken)
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

        var now = _timeProvider.GetUtcNow();

        card.Close(now);

        portfolio.ReleaseLien(card.LienId, now);

        if (portfolio.Status == PortfolioStatus.Frozen)
        {
            var hasOtherDelinquentCards =
                await _creditCardRepository.HasOtherDelinquentCardsAsync(
                    portfolio.Id,
                    card.Id,
                    cancellationToken);

            if (!hasOtherDelinquentCards)
            {
                portfolio.UnfreezeAfterDelinquencyResolution();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

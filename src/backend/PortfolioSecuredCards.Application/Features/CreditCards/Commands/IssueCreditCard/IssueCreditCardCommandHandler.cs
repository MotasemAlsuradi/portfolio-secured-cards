
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Domain.Entities;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.IssueCreditCard;

public sealed class IssueCreditCardCommandHandler
    : IRequestHandler<IssueCreditCardCommand, Guid>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentCustomer _currentCustomer;
    private readonly TimeProvider _timeProvider;

    public IssueCreditCardCommandHandler(IPortfolioRepository portfolioRepository,ICreditCardRepository creditCardRepository, IUnitOfWork unitOfWork,
        ICurrentCustomer currentCustomer, TimeProvider timeProvider)
    {
        _portfolioRepository = portfolioRepository;
        _creditCardRepository = creditCardRepository;
        _unitOfWork = unitOfWork;
        _currentCustomer = currentCustomer;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(IssueCreditCardCommand request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(
            request.PortfolioId,
            cancellationToken);

        if (portfolio is null ||
            portfolio.CustomerId != _currentCustomer.CustomerId)
        {
            throw new KeyNotFoundException("Portfolio was not found.");
        }

        var now = _timeProvider.GetUtcNow();

        var lien = portfolio.ReserveLien(request.LienAmount, now);

        var card = CreditCard.Issue(
            portfolio.Id,
            lien.Id,
            lien.Amount,
            request.CreditLimit,
            GenerateCardNumber(),
            now);

        await _creditCardRepository.AddAsync(card, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return card.Id;
    }

    private static string GenerateCardNumber()
    {
        return Guid.NewGuid().ToString("N")[..30];
    }
}
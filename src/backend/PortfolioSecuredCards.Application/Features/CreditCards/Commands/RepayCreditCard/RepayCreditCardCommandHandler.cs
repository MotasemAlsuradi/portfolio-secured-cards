
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Persistence;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.RepayCreditCard;

public sealed class RepayCreditCardCommandHandler
    : IRequestHandler<RepayCreditCardCommand, decimal>
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentCustomer _currentCustomer;

    public RepayCreditCardCommandHandler(ICreditCardRepository creditCardRepository, IPortfolioRepository portfolioRepository, IUnitOfWork unitOfWork, 
        ICurrentCustomer currentCustomer)
    {
        _creditCardRepository = creditCardRepository;
        _portfolioRepository = portfolioRepository;
        _unitOfWork = unitOfWork;
        _currentCustomer = currentCustomer;
    }

    public async Task<decimal> Handle(RepayCreditCardCommand request, CancellationToken cancellationToken)
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

        card.Repay(request.Amount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return card.UtilizedAmount;
    }
}
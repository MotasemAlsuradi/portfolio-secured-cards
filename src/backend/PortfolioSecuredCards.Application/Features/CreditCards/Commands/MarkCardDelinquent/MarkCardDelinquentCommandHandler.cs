
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Application.Configuration;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.MarkCardDelinquent;

public sealed class MarkCardDelinquentCommandHandler : IRequestHandler<MarkCardDelinquentCommand, bool>
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DelinquencyOptions _options;

    public MarkCardDelinquentCommandHandler(ICreditCardRepository creditCardRepository, IPortfolioRepository portfolioRepository, 
        IUnitOfWork unitOfWork, DelinquencyOptions options)
    {
        _creditCardRepository = creditCardRepository;
        _portfolioRepository = portfolioRepository;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<bool> Handle(MarkCardDelinquentCommand request, CancellationToken cancellationToken)
    {
        var card = await _creditCardRepository.GetByIdAsync(
            request.CardId,
            cancellationToken);

        if (card is null)
        {
            throw new KeyNotFoundException($"Credit card {request.CardId} was not found.");
        }

        var portfolio = await _portfolioRepository.GetByIdAsync(
            card.PortfolioId,
            cancellationToken);

        if (portfolio is null)
        {
            throw new KeyNotFoundException($"Portfolio was not found for card {card.Id}.");
        }

        var changed = card.MarkDelinquent(request.ScanTime, _options.Threshold);

        if (!changed)
        {
            return false;
        }

        portfolio.Freeze();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
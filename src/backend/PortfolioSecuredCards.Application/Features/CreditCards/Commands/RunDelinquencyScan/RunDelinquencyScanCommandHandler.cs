
using MediatR;
using Microsoft.Extensions.Logging;
using PortfolioSecuredCards.Application.Abstractions.Persistence;
using PortfolioSecuredCards.Application.Abstractions.Processing;

namespace PortfolioSecuredCards.Application.Features.CreditCards.Commands.RunDelinquencyScan;

public sealed class RunDelinquencyScanCommandHandler : IRequestHandler<RunDelinquencyScanCommand, int>
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICardDelinquencyProcessor _processor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RunDelinquencyScanCommandHandler> _logger;

    public RunDelinquencyScanCommandHandler(ICreditCardRepository creditCardRepository, ICardDelinquencyProcessor processor, TimeProvider timeProvider, 
        ILogger<RunDelinquencyScanCommandHandler> logger)
    {
        _creditCardRepository = creditCardRepository;
        _processor = processor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(RunDelinquencyScanCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var cards = await _creditCardRepository.GetFullyUtilizedActiveCardsAsync(cancellationToken);

        var delinquentCount = 0;

        foreach (var cardId in cards.Select(card => card.Id).ToList())
        {
            try
            {
                var changed = await _processor.ProcessAsync(
                    cardId,
                    now,
                    cancellationToken);

                if (changed)
                {
                    delinquentCount++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Failed to process delinquency for card {CardId}.", 
                    cardId);
            }
        }

        return delinquentCount;
    }
}
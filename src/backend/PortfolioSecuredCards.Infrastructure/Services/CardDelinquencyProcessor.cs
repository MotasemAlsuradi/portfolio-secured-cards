
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PortfolioSecuredCards.Application.Abstractions.Processing;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.MarkCardDelinquent;

namespace PortfolioSecuredCards.Infrastructure.Services;

public sealed class CardDelinquencyProcessor : ICardDelinquencyProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CardDelinquencyProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> ProcessAsync(Guid cardId, DateTimeOffset scanTime, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(
            new MarkCardDelinquentCommand(cardId, scanTime),
            cancellationToken);
    }
}
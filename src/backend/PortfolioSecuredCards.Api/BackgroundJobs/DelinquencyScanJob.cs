
using MediatR;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.RunDelinquencyScan;

namespace PortfolioSecuredCards.Api.BackgroundJobs;

public sealed class DelinquencyScanJob
{
    private readonly ISender _sender;
    private readonly ILogger<DelinquencyScanJob> _logger;

    public DelinquencyScanJob(ISender sender, ILogger<DelinquencyScanJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Delinquency scan started.");

        var count = await _sender.Send(new RunDelinquencyScanCommand());
        _logger.LogInformation("Delinquency scan completed. {Count} cards marked delinquent.", count);
    }
}
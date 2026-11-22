
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.RunDelinquencyScan;

namespace PortfolioSecuredCards.Api.Controllers;

[ApiController]
[Route("api/delinquency")]
public sealed class  DelinquencyController : ControllerBase
{
    private readonly ISender _sender;

    public DelinquencyController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> RunScan(CancellationToken cancellationToken)
    {
        var delinquentCount = await _sender.Send(
            new RunDelinquencyScanCommand(),
            cancellationToken);

        return Ok(new { DelinquentCount = delinquentCount });
    }
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using PortfolioSecuredCards.Api.Contracts.Portfolios;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Features.Portfolios.Commands.RedeemPortfolio;
using PortfolioSecuredCards.Application.Features.Portfolios.Queries.GetCustomerPortfolios;

namespace PortfolioSecuredCards.Api.Controllers;

[ApiController]
[Route("api/portfolios")]
public sealed class PortfoliosController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentCustomer _currentCustomer;

    public PortfoliosController(ISender sender, ICurrentCustomer currentCustomer)
    {
        _sender = sender;
        _currentCustomer = currentCustomer;
    }

    [HttpGet]
    public async Task<IActionResult> GetPortfolios(CancellationToken cancellationToken)
    {
        var portfolios = await _sender.Send(
            new GetCustomerPortfoliosQuery(_currentCustomer.CustomerId),
            cancellationToken);

        return Ok(portfolios);
    }

    [HttpPost("{portfolioId:guid}/redeem")]
    public async Task<IActionResult> Redeem(Guid portfolioId, [FromBody] RedeemPortfolioRequest request, CancellationToken cancellationToken)
    {
        var availableValue = await _sender.Send(
            new RedeemPortfolioCommand(portfolioId, request.Amount),
            cancellationToken);

        return Ok(new { AvailableValue = availableValue });
    }
}


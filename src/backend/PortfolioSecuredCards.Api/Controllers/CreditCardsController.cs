
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PortfolioSecuredCards.Api.Contracts.CreditCards;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.CloseCreditCard;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.IssueCreditCard;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.RepayCreditCard;
using PortfolioSecuredCards.Application.Features.CreditCards.Commands.UseCreditCard;
using PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCardUsageHistory;
using PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCreditCardById;
using PortfolioSecuredCards.Application.Features.CreditCards.Queries.GetCustomerCreditCards;

namespace PortfolioSecuredCards.Api.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly ISender _sender;

    public CreditCardsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCards(CancellationToken cancellationToken)
    {
        var cards = await _sender.Send(
            new GetCustomerCreditCardsQuery(),
            cancellationToken);

        return Ok(cards);
    }

    [HttpGet("{cardId:guid}")]
    public async Task <IActionResult> GetCardById(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await _sender.Send(
            new GetCreditCardByIdQuery(cardId),
            cancellationToken);

        return Ok(card);
    }

    [HttpGet("{cardId:guid}/usage-history")]
    public async Task<IActionResult> GetUsageHistory(Guid cardId, CancellationToken cancellationToken)
    {
        var history = await _sender.Send(
            new GetCardUsageHistoryQuery(cardId),
            cancellationToken);

        return Ok(history);
    }

    [HttpPost]
    public async Task<IActionResult> IssueCard([FromBody] IssueCreditCardRequest request, CancellationToken cancellationToken)
    {
        var cardId = await _sender.Send(
            new IssueCreditCardCommand(
                request.PortfolioId,
                request.LienAmount,
                request.CreditLimit),
            cancellationToken);

        return CreatedAtAction(nameof(GetCardById), new { cardId }, new { CardId = cardId });
    }

    [HttpPost("{cardId:guid}/use")]
    public async Task<IActionResult> UseCard(Guid cardId, [FromBody] CreditCardAmountRequest request, CancellationToken cancellationToken)
    {
        var availableCredit = await _sender.Send(
            new UseCreditCardCommand(cardId, request.Amount),
            cancellationToken);

        return Ok(new { AvailableCredit = availableCredit });
    }

    [HttpPost("{cardId:guid}/repay")]
    public async Task<IActionResult> RepayCard(Guid cardId, [FromBody] CreditCardAmountRequest request, CancellationToken cancellationToken)
    {
        var utilizedAmount = await _sender.Send(
            new RepayCreditCardCommand(cardId, request.Amount),
            cancellationToken);

        return Ok(new { UtilizedAmount = utilizedAmount });
    }

    [HttpPost("{cardId:guid}/close")]
    public async Task<IActionResult> CloseCard(Guid cardId, CancellationToken cancellationToken)
    {
        await _sender.Send(
            new CloseCreditCardCommand(cardId),
            cancellationToken);

        return NoContent();
    }
}
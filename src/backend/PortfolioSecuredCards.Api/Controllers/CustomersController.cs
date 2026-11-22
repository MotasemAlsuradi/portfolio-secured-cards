
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PortfolioSecuredCards.Application.Features.Customers.Queries.GetCustomers;

namespace PortfolioSecuredCards.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await _sender.Send(
            new GetCustomersQuery(), 
            cancellationToken);

        return Ok(customers);
    }
}
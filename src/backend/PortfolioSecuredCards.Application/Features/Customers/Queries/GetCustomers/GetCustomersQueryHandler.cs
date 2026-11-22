
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Queries;

namespace PortfolioSecuredCards.Application.Features.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    private readonly ICustomerReadService _customerReadService;

    public GetCustomersQueryHandler(ICustomerReadService customerReadService)
    {
        _customerReadService = customerReadService;
    }

    public async Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        return await _customerReadService.GetAllAsync(cancellationToken);
    }
}
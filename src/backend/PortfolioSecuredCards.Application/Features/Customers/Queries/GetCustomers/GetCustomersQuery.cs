
using MediatR;

namespace PortfolioSecuredCards.Application.Features.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery() : IRequest<IReadOnlyList<CustomerDto>>;
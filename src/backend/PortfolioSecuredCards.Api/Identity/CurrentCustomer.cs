
using PortfolioSecuredCards.Application.Abstractions.Identity;

namespace PortfolioSecuredCards.Api.Identity;

public sealed class CurrentCustomer : ICurrentCustomer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentCustomer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CustomerId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                throw new InvalidOperationException(
                    "Current customer is only available within an HTTP request.");
            }

            var customerIdHeader =
                httpContext.Request.Headers["X-Demo-Customer-Id"].ToString();

            if (!Guid.TryParse(customerIdHeader, out var customerId) ||
                customerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A valid X-Demo-Customer-Id header is required.");
            }

            return customerId;
        }
    }
}
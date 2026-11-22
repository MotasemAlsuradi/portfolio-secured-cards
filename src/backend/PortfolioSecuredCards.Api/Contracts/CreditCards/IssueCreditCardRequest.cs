
namespace PortfolioSecuredCards.Api.Contracts.CreditCards;

public sealed record IssueCreditCardRequest(Guid PortfolioId, decimal LienAmount, decimal CreditLimit);
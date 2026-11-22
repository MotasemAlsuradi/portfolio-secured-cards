
using MediatR;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Abstractions.Persistence;

namespace PortfolioSecuredCards.Application.Features.Portfolios.Commands.RedeemPortfolio;

public sealed class RedeemPortfolioCommandHandler : IRequestHandler<RedeemPortfolioCommand, decimal>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentCustomer _currentCustomer;

    public RedeemPortfolioCommandHandler(IPortfolioRepository portfolioRepository, IUnitOfWork unitOfWork, ICurrentCustomer currentCustomer)
    {
        _portfolioRepository = portfolioRepository;
        _unitOfWork = unitOfWork;
        _currentCustomer = currentCustomer;
    }

    public async Task<decimal> Handle(RedeemPortfolioCommand request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(
            request.PortfolioId,
            cancellationToken);

        if (portfolio is null ||
            portfolio.CustomerId != _currentCustomer.CustomerId)
        {
            throw new KeyNotFoundException("Portfolio was not found.");
        }

        portfolio.Redeem(request.Amount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return portfolio.AvailableValue;
    }
}

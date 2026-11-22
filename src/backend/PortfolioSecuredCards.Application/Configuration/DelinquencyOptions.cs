
namespace PortfolioSecuredCards.Application.Configuration;

public sealed class DelinquencyOptions
{
    public int ThresholdMinutes { get; set; } = 1440;

    public TimeSpan Threshold => TimeSpan.FromMinutes(ThresholdMinutes);
}
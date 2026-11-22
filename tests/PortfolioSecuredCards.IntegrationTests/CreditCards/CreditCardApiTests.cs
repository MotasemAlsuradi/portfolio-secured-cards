using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PortfolioSecuredCards.IntegrationTests.CreditCards;

public sealed class CreditCardApiTests : IDisposable
{
    private const string CustomerHeader = "X-Demo-Customer-Id";
    private const string DemoCustomerId = "11111111-1111-1111-1111-111111111111";

    private const int BasicPortfolioType = 1;
    private const int ClosedCardStatus = 4;

    private const decimal LienAmount = 5_000m;
    private const decimal CreditLimit = 4_000m;

    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public CreditCardApiTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add(CustomerHeader, DemoCustomerId);
    }

    [Fact]
    public async Task IssueCreditCard_ShouldCreateCardAndReserveLien()
    {
        // Arrange
        var portfolioId = await GetPortfolioId();
        var before = await GetPortfolio(portfolioId);

        // Act
        var response = await IssueCard(portfolioId);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var card = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, card.GetProperty("cardId").GetGuid());

        var after = await GetPortfolio(portfolioId);
        Assert.Equal(before.TotalLienAmount + LienAmount, after.TotalLienAmount);
        Assert.Equal(before.AvailableValue - LienAmount, after.AvailableValue);
    }

    [Fact]
    public async Task RedeemPortfolio_WithNegativeAmount_ShouldReturn400()
    {
        // Arrange
        var portfolioId = await GetPortfolioId();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolioId}/redeem",
            new { Amount = -100 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InvalidAmount", error.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreditCardLifecycle_ShouldAllowUseRepayAndClose()
    {
        // Arrange
        var portfolioId = await GetPortfolioId();
        var before = await GetPortfolio(portfolioId);

        var issueResponse = await IssueCard(portfolioId);
        Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);

        var cardId = (await issueResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("cardId")
            .GetGuid();

        // Act
        var useResponse = await _client.PostAsJsonAsync(
            $"/api/credit-cards/{cardId}/use",
            new { Amount = CreditLimit });

        var repayResponse = await _client.PostAsJsonAsync(
            $"/api/credit-cards/{cardId}/repay",
            new { Amount = CreditLimit });

        var closeResponse = await _client.PostAsync(
            $"/api/credit-cards/{cardId}/close",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, useResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repayResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, closeResponse.StatusCode);

        var closedCard = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/credit-cards/{cardId}");
        Assert.Equal(ClosedCardStatus, closedCard.GetProperty("status").GetInt32());

        var after = await GetPortfolio(portfolioId);
        Assert.Equal(before.TotalLienAmount, after.TotalLienAmount);
        Assert.Equal(before.AvailableValue, after.AvailableValue);
    }

    private Task<HttpResponseMessage> IssueCard(Guid portfolioId)
    {
        return _client.PostAsJsonAsync(
            "/api/credit-cards",
            new
            {
                PortfolioId = portfolioId,
                LienAmount,
                CreditLimit
            });
    }

    private async Task<Guid> GetPortfolioId()
    {
        var portfolios = await _client.GetFromJsonAsync<JsonElement>("/api/portfolios");

        return portfolios.EnumerateArray()
            .First(x => x.GetProperty("type").GetInt32() == BasicPortfolioType)
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<PortfolioSnapshot> GetPortfolio(Guid portfolioId)
    {
        var portfolios = await _client.GetFromJsonAsync<JsonElement>("/api/portfolios");

        var portfolio = portfolios.EnumerateArray()
            .First(x => x.GetProperty("id").GetGuid() == portfolioId);

        return new PortfolioSnapshot(
            portfolio.GetProperty("totalLienAmount").GetDecimal(),
            portfolio.GetProperty("availableValue").GetDecimal());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private sealed record PortfolioSnapshot(decimal TotalLienAmount, decimal AvailableValue);
}
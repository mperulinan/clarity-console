using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ExternalServices;

namespace Portfolio.Infrastructure.Tests.ExternalServices;

public class FrankfurterExchangeRateProviderTests
{
    [Fact]
    public async Task GetUsdEurRateAsync_ShouldThrowNotSupportedException_WhenDateIsTodayOrFuture()
    {
        // Arrange
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder().Build();
        var provider = new FrankfurterExchangeRateProvider(httpClient, config, NullLogger<FrankfurterExchangeRateProvider>.Instance);

        var today = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetUsdEurRateAsync(today));
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetUsdEurRateAsync(today.AddDays(1)));
    }

    [Fact]
    public async Task GetEurUsdRateAsync_ShouldThrowNotSupportedException_WhenDateIsTodayOrFuture()
    {
        // Arrange
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder().Build();
        var provider = new FrankfurterExchangeRateProvider(httpClient, config, NullLogger<FrankfurterExchangeRateProvider>.Instance);

        var today = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetEurUsdRateAsync(today));
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetEurUsdRateAsync(today.AddDays(1)));
    }
}

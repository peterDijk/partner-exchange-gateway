using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Tests.Domain;

public class RetryPolicyTests
{
    private static readonly RetryPolicy Policy = new(
        6, TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(30));

    [Theory]
    [InlineData(1, 0.0, 500)]
    [InlineData(1, 1.0, 1_000)]
    [InlineData(3, 1.0, 4_000)]
    [InlineData(10, 1.0, 30_000)]   // capped
    [InlineData(10, 0.0, 15_000)]   // capped, minimum half
    public void Delay_is_exponential_capped_and_jittered(int attempt, double jitter, int expectedMs)
    {
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), Policy.DelayFor(attempt, jitter));
    }
}

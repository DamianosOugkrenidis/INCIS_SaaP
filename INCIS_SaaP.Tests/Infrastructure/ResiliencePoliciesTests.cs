using FluentAssertions;
using INCIS_SaaP.Infrastructure.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Infrastructure;

/// <summary>
/// Tests for retry, timeout and circuit-breaker resilience policies.
/// </summary>
public class ResiliencePoliciesTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_ReturnsResult_OnFirstSuccess()
    {
        var calls = 0;
        var result = await ResiliencePolicies.ExecuteWithRetryAsync(() =>
        {
            calls++;
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesOnTransientException_ThenSucceeds()
    {
        var calls = 0;
        var result = await ResiliencePolicies.ExecuteWithRetryAsync(
            () =>
            {
                calls++;
                if (calls < 3)
                    throw new TimeoutException("transient");
                return Task.FromResult("ok");
            },
            maxRetries: 5,
            initialDelay: TimeSpan.FromMilliseconds(1));

        result.Should().Be("ok");
        calls.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DoesNotRetry_OnNonTransientException()
    {
        var calls = 0;
        Func<Task> act = () => ResiliencePolicies.ExecuteWithRetryAsync<int>(
            () =>
            {
                calls++;
                throw new InvalidOperationException("permanent");
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(1));

        await act.Should().ThrowAsync<InvalidOperationException>();
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ReturnsResult_WhenFast()
    {
        var result = await ResiliencePolicies.ExecuteWithTimeoutAsync(
            _ => Task.FromResult(7),
            TimeSpan.FromSeconds(5));

        result.Should().Be(7);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_Throws_WhenExceeded()
    {
        Func<Task> act = () => ResiliencePolicies.ExecuteWithTimeoutAsync<int>(
            async ct => { await Task.Delay(5000, ct); return 1; },
            TimeSpan.FromMilliseconds(30),
            NullLogger.Instance,
            "slow-op");

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterThreshold_AndRejects()
    {
        var breaker = new CircuitBreaker("test", failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(50));

        breaker.State.Should().Be(CircuitState.Closed);

        // Two failures -> opens
        for (var i = 0; i < 2; i++)
        {
            Func<Task> fail = () => breaker.ExecuteAsync<int>(() => throw new Exception("boom"));
            await fail.Should().ThrowAsync<Exception>();
        }

        breaker.State.Should().Be(CircuitState.Open);

        // Next call rejected without executing
        Func<Task> rejected = () => breaker.ExecuteAsync(() => Task.FromResult(1));
        await rejected.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task CircuitBreaker_RecoversToClosed_AfterOpenDuration()
    {
        var breaker = new CircuitBreaker("recover", failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(40));

        Func<Task> fail = () => breaker.ExecuteAsync<int>(() => throw new Exception("boom"));
        await fail.Should().ThrowAsync<Exception>();
        breaker.State.Should().Be(CircuitState.Open);

        await Task.Delay(70);
        breaker.State.Should().Be(CircuitState.HalfOpen);

        var result = await breaker.ExecuteAsync(() => Task.FromResult(99));
        result.Should().Be(99);
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void CircuitBreakerFactory_ReturnsSameInstance_PerName()
    {
        var factory = new CircuitBreakerFactory(NullLoggerFactory.Instance);

        var a = factory.GetOrCreate("svc-a");
        var b = factory.GetOrCreate("svc-a");
        var c = factory.GetOrCreate("svc-b");

        a.Should().BeSameAs(b);
        a.Should().NotBeSameAs(c);
    }
}

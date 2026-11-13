using FluentAssertions;
using INCIS_SaaP.Architecture.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Architecture;

/// <summary>
/// Tests for the in-process message bus (publish/subscribe + envelope).
/// </summary>
public class MessageBusTests
{
    public sealed class SampleMessage
    {
        public string Text { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private static InProcessMessageBus CreateBus()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        return new InProcessMessageBus(provider, NullLogger<InProcessMessageBus>.Instance);
    }

    [Fact]
    public async Task PublishAsync_DeliversMessage_ToTypedSubscriber()
    {
        using var bus = CreateBus();
        var tcs = new TaskCompletionSource<SampleMessage>();

        bus.Subscribe<SampleMessage>((msg, _) =>
        {
            tcs.TrySetResult(msg);
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new SampleMessage { Text = "hello", Value = 5 });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        completed.Should().Be(tcs.Task, "the subscriber should receive the message");
        var received = await tcs.Task;
        received.Text.Should().Be("hello");
        received.Value.Should().Be(5);
    }

    [Fact]
    public async Task PublishAsync_ToTopic_DeliversEnvelope()
    {
        using var bus = CreateBus();
        var tcs = new TaskCompletionSource<MessageEnvelope>();

        bus.Subscribe("custom-topic", (env, _) =>
        {
            tcs.TrySetResult(env);
            return Task.CompletedTask;
        });

        await bus.PublishAsync("custom-topic", new SampleMessage { Text = "topic-msg" });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        completed.Should().Be(tcs.Task);
        var envelope = await tcs.Task;
        envelope.Payload.Should().Contain("topic-msg");
        envelope.MessageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Subscribe_Dispose_StopsDelivery()
    {
        using var bus = CreateBus();
        var received = 0;

        var subscription = bus.Subscribe("t", (_, _) =>
        {
            Interlocked.Increment(ref received);
            return Task.CompletedTask;
        });

        subscription.Dispose();

        await bus.PublishAsync("t", new SampleMessage { Text = "x" });
        await Task.Delay(200); // give the background processor time

        received.Should().Be(0);
    }

    [Fact]
    public void MessageEnvelope_HasSensibleDefaults()
    {
        var envelope = new MessageEnvelope();

        envelope.MessageId.Should().NotBe(Guid.Empty);
        envelope.MaxRetries.Should().Be(3);
        envelope.RetryCount.Should().Be(0);
        envelope.Headers.Should().NotBeNull();
        envelope.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

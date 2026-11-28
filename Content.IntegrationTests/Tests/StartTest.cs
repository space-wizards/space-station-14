#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.Exceptions;

namespace Content.IntegrationTests.Tests;

public sealed class StartTest : GameTest
{
    [SidedDependency(Side.Client)] private IRuntimeLog _cRuntimeLog = null!;
    [SidedDependency(Side.Server)] private IRuntimeLog _sRuntimeLog = null!;

    /// <summary>
    /// Test that the server and client start and stop.
    /// </summary>
    [Test]
    [Description("Test that the server and client start.")]
    public async Task TestClientStart()
    {
        Assert.That(Client.IsAlive);
        await Client.WaitRunTicks(5);
        Assert.That(Client.IsAlive);
        Assert.That(_cRuntimeLog.ExceptionCount, Is.Zero, "No exceptions must be logged on client.");
        await Client.WaitIdleAsync();
        Assert.That(Client.IsAlive);

        Assert.That(Server.IsAlive);
        Assert.That(_sRuntimeLog.ExceptionCount, Is.Zero, "No exceptions must be logged on server.");
        await Server.WaitIdleAsync();
        Assert.That(Server.IsAlive);
    }
}

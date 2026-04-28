#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC.Exceptions;

namespace Content.IntegrationTests.Tests.GameTestTests;

[TestOf(typeof(GameTest))]
[TestOf(typeof(SidedDependencyAttribute))]
public sealed class DisconnectedDependencyTest : GameTest
{
    [Test]
    [PairConfig(nameof(PsDisconnected))]
    [Description($"""
            Ensures that GameTest can be started up even when the client {nameof(EntitySystemManager)} isn't initialized yet.
            No body is necessary, if this fails then the bug is firmly in GameTest itself.
        """)]
    public void EnsureGameTestSetupWorksDisconnected()
    {
        // Nothin'
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    [Description("""
            Ensures dependency injection that relies on client side systems fails as expected when the client is detached.
        """)]
    public void ClientSystemDependencyFails()
    {
        var creature = new Creature();

        Assert.Throws<UnregisteredTypeException>(() =>
        {
            InjectDependencies(creature);
        });
    }

    private sealed class Creature
    {
#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SidedDependency(Side.Client)] private readonly MapSystem _mapSys = null!;
#pragma warning restore CS0414 // Field is assigned but its value is never used
    }
}

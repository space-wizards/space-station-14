using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.GameObjects;

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
}

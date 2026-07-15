using Content.IntegrationTests.Fixtures;
using Content.Server.Destructible;
using Content.Shared.Destructible;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible;

/// <summary>
/// Tests ensuring the correct operation of <see cref="SharedDestructibleSystem"/>.
/// </summary>
public sealed class DestructibleSquishTest : GameTest
{
    /// <summary>
    /// Test that multiple thresholds of the same trigger combine into a single threshold.
    /// </summary>
    [Test]
    [TestOf(typeof(DestructibleSystem))]
    public async Task EnsureSquish()
    {
        var testMap = await Pair.CreateTestMap();
        DestructibleComponent sDestructible = default;

        // Spawn our test entity
        await Server.WaitPost(() =>
        {
            var ent = SSpawnAtPosition(DestructibleThresholdSquishEntityId, testMap.GridCoords);
            sDestructible = SEntMan.EnsureComponent<DestructibleComponent>(ent);
        });

        // Assert that the thresholds shrunk from 4 on the prototype to 2 on the entity
        // Assert that both thresholds have the 2 behaviors from the prototype
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sDestructible.Thresholds, Has.Count.EqualTo(2));
            Assert.That(sDestructible.Thresholds[0].Behaviors, Has.Count.EqualTo(2));
            Assert.That(sDestructible.Thresholds[1].Behaviors, Has.Count.EqualTo(2));
        }
    }
}

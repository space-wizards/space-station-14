using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Alert;
using Content.Shared.Mobs.Components;

namespace Content.IntegrationTests.Tests.Damageable;

public sealed class MobThresholdsTest : GameTest
{
    private static string[] _entitiesWithThresholds = GameDataScrounger.EntitiesWithComponent("MobThresholds");

    [Test]
    [TestOf(typeof(MobThresholdsComponent))]
    [TestCaseSource(nameof(_entitiesWithThresholds))]
    [Description("Ensures every entity with mob thresholds has valid mob state configuration corresponding to some AlertPrototype.")]
    public async Task ValidateMobThresholds(string protoKey)
    {
        var pair = Pair;
        var server = pair.Server;

        var protoMan = server.ProtoMan;

        Assert.Multiple(() =>
        {
            var proto = protoMan.Index(protoKey);
            var comp = (MobThresholdsComponent)proto.Components["MobThresholds"].Component;

            // See which mob states are mapped to alerts
            var alertStates = comp.StateAlertDict.Keys;
            // Check each mob state that this mob can be in
            foreach (var (_, state) in comp.Thresholds)
            {
                // Make sure that an alert exists for each possible mob state
                Assert.That(alertStates, Does.Contain(state), $"{proto.ID} does not have an alert state for mob state {state}");
            }
        });
    }
}

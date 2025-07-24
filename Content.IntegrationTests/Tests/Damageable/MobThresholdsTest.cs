using Content.Shared.Alert;
using Content.Shared.Mobs.Components;

namespace Content.IntegrationTests.Tests.Damageable;

public sealed class MobThresholdsTest
{
    /// <summary>
    /// Inspects every entity prototype with a <see cref="MobThresholdsComponent"/> and makes
    /// sure that every possible mob state is mapped to an <see cref="AlertPrototype"/>.
    /// </summary>
    [Test]
    public async Task ValidateMobThresholds()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;

        var protos = pair.GetPrototypesWithComponent<MobThresholdsComponent>();

        Assert.Multiple(() =>
        {
            foreach (var (proto, comp) in protos)
            {
                // See which mob states are mapped to alerts
                var alertStates = comp.StateAlertDict.Keys;
                // Check each mob state that this mob can be in
                foreach (var (_, state) in comp.Thresholds)
                {
                    // Make sure that an alert exists for each possible mob state
                    Assert.That(alertStates, Does.Contain(state), $"{proto.ID} does not have an alert state for mob state {state}");
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}

using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared.Power.Components;
using Content.Shared.NodeContainer;
using Robust.Shared.EntitySerialization;

namespace Content.IntegrationTests.Tests.Power;

[Explicit]
public sealed class StationPowerTests
{
    /// <summary>
    /// How long the station should be able to survive on stored power if nothing is changed from round start.
    /// </summary>
    private const float MinimumPowerDurationSeconds = 10 * 60;

    private static readonly string[] GameMaps =
    [
        "Fland",
        "Meta",
        "Packed",
        "Omega",
        "Bagel",
        "Box",
        "Core",
        "Marathon",
        "Saltern",
        "Reach",
        "Train",
        "Oasis",
        "Gate",
        "Amber",
        "Loop",
        "Plasma",
        "Elkridge",
        "Convex",
        "Relic",
    ];

    [Test, TestCaseSource(nameof(GameMaps))]
    public async Task TestStationStartingPowerWindow(string mapProtoId)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
        });
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var ticker = entMan.System<GameTicker>();

        // Load the map
        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            ticker.LoadGameMap(mapProto, out var mapId, opts);
        });

        // Let powernet set up
        await server.WaitRunTicks(1);

        // Find the power network with the greatest stored charge in its batteries.
        // This keeps backup SMESes out of the calculation.
        var networks = new Dictionary<PowerState.Network, float>();
        var batteryQuery = entMan.EntityQueryEnumerator<PowerNetworkBatteryComponent, BatteryComponent, NodeContainerComponent>();
        while (batteryQuery.MoveNext(out var uid, out _, out var battery, out var nodeContainer))
        {
            if (!nodeContainer.Nodes.TryGetValue("output", out var node))
                continue;
            if (node.NodeGroup is not IBasePowerNet group)
                continue;
            networks.TryGetValue(group.NetworkNode, out var charge);
            networks[group.NetworkNode] = charge + battery.CurrentCharge;
        }
        var totalStartingCharge = networks.MaxBy(n => n.Value).Value;

        // Find how much charge all the APC-connected devices would like to use per second.
        var totalAPCLoad = 0f;
        var receiverQuery = entMan.EntityQueryEnumerator<ApcPowerReceiverComponent>();
        while (receiverQuery.MoveNext(out _, out var receiver))
        {
            totalAPCLoad += receiver.Load;
        }

        var estimatedDuration = totalStartingCharge / totalAPCLoad;
        var requiredStoredPower = totalAPCLoad * MinimumPowerDurationSeconds;
        Assert.Multiple(() =>
        {
            Assert.That(estimatedDuration, Is.GreaterThanOrEqualTo(MinimumPowerDurationSeconds),
                $"Initial power for {mapProtoId} does not last long enough! Needs at least {MinimumPowerDurationSeconds}s " +
                $"but estimated to last only {estimatedDuration}s!");
            Assert.That(totalStartingCharge, Is.GreaterThanOrEqualTo(requiredStoredPower),
                $"Needs at least {requiredStoredPower - totalStartingCharge} more stored power!");
        });


        await pair.CleanReturnAsync();
    }
}

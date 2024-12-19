using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Robust.Server.GameObjects;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture]
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
        "Cluster",
        "Omega",
        "Bagel",
        "Origin",
        "Box",
        "Saltern",
        "Core",
        "Marathon",
        "Atlas",
        "Reach",
        "Train",
        "Oasis"
    ];

    [Test, TestCaseSource(nameof(GameMaps))]
    public async Task TestStationStartingPowerWindow(string mapProtoId)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var mapSystem = entMan.System<MapSystem>();
        var ticker = entMan.System<GameTicker>();

        // Load the map
        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out var mapId);

            Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
            ticker.LoadGameMap(mapProto, mapId, null);
        });

        // Let powernet set up
        await server.WaitRunTicks(1);

        // Find the power network with the greatest stored charge in its SMESes.
        // This keeps backup SMESes out of the calculation.
        var networks = new Dictionary<PowerState.Network, float>();
        var smesQuery = entMan.EntityQueryEnumerator<PowerNetworkBatteryComponent, BatteryComponent, NodeContainerComponent>();
        while (smesQuery.MoveNext(out var uid, out _, out var battery, out var nodeContainer))
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

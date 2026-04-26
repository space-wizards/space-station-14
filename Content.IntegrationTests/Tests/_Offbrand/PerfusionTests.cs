using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Body.Components;
using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.FixedPoint;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Offbrand;

[TestFixture]
public sealed class PerfusionTests : GameTest
{
    private static EntProtoId MobHuman = "MobHuman";
    private static ResPath TestGrid = new("Maps/Test/Breathing/3by3-20oxy-80nit.yml");

    [SidedDependency(Side.Server)] private readonly PerfusionSystem _perfusion = default!;
    [SidedDependency(Side.Server)] private readonly MapLoaderSystem _mapLoader = default!;

    [Test]
    public async Task SanityCheck()
    {
        Pair.TestMap = await Pair.CreateTestMap();

        Entity<MapGridComponent> grid = default!;
        await Server.WaitAssertion(() =>
        {
            Assert.That(_mapLoader.TryLoadGrid(TestMap!.MapId, TestGrid, out var gridEnt));
            grid = gridEnt!.Value;
        });

        var center = new Vector2(0.5f, 0.5f);
        var coordinates = new EntityCoordinates(grid, center);

        EntityUid mob = default!;
        RespiratorComponent respirator = default!;
        PerfusionComponent perfusion = default!;
        await Server.WaitAssertion(() =>
        {
            mob = SSpawnAtPosition(MobHuman, coordinates);
            respirator = SComp<RespiratorComponent>(mob);
            perfusion = SComp<PerfusionComponent>(mob);
        });

        // let us take The Breath

        await PoolManager.WaitUntil(Server, () => respirator.Status == RespiratorStatus.Exhaling);
        await PoolManager.WaitUntil(Server, () => respirator.Status == RespiratorStatus.Inhaling);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_perfusion.Spo2(mob), Is.EqualTo(FixedPoint2.New(1)));
            SQuerySingle<OffbrandLungOrganComponent>(out var organ);
            SDeleteNow(organ!.Value);
        });

        var lastUpdate = perfusion.LastUpdate;
        await PoolManager.WaitUntil(Server, () => perfusion.LastUpdate != lastUpdate);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_perfusion.Spo2(mob), Is.LessThanOrEqualTo(FixedPoint2.New(0.05d)));
        });
    }
}

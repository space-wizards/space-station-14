using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests.Station;

[TestFixture]
[TestOf(typeof(EmergencyShuttleSystem))]
public sealed class EvacShuttleTest
{
    /// <summary>
    /// Ensure that the emergency shuttle can be called, and that it will travel to centcomm
    /// </summary>
    [Test]
    public async Task EmergencyEvacTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { DummyTicker = true, Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();

        // Dummy ticker tests should not have centcomm
        Assert.That(entMan.Count<StationCentcommComponent>(), Is.Zero);

        var shuttleEnabled = pair.Server.CfgMan.GetCVar(CCVars.EmergencyShuttleEnabled);
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, "Saltern");
        pair.Server.CfgMan.SetCVar(CCVars.GameDummyTicker, false);
        pair.Server.CfgMan.SetCVar(CCVars.EmergencyShuttleEnabled, true);

        await server.WaitPost(() => ticker.RestartRound());
        await pair.RunTicksSync(25);
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

        // Find the station, centcomm, and shuttle, and ftl map.

        Assert.That(entMan.Count<StationCentcommComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<StationEmergencyShuttleComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<StationDataComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<EmergencyShuttleComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<FTLMapComponent>(), Is.EqualTo(0));

        var station = (Entity<StationCentcommComponent>) entMan.AllComponentsList<StationCentcommComponent>().Single();
        var data = entMan.GetComponent<StationDataComponent>(station);
        var shuttleData = entMan.GetComponent<StationEmergencyShuttleComponent>(station);

        var saltern = data.Grids.Single();
        Assert.That(entMan.HasComponent<MapGridComponent>(saltern));

        var shuttle = shuttleData.EmergencyShuttle!.Value;
        Assert.That(entMan.HasComponent<EmergencyShuttleComponent>(shuttle));
        Assert.That(entMan.HasComponent<MapGridComponent>(shuttle));

        var centcomm = station.Comp.Entity!.Value;
        Assert.That(entMan.HasComponent<MapGridComponent>(centcomm));

        var centcommMap = station.Comp.MapEntity!.Value;
        Assert.That(entMan.HasComponent<MapComponent>(centcommMap));
        Assert.That(server.Transform(centcomm).MapUid, Is.EqualTo(centcommMap));

        var salternXform = server.Transform(saltern);
        Assert.That(salternXform.MapUid, Is.Not.Null);
        Assert.That(salternXform.MapUid, Is.Not.EqualTo(centcommMap));

        var shuttleXform = server.Transform(shuttle);
        Assert.That(shuttleXform.MapUid, Is.Not.Null);
        Assert.That(shuttleXform.MapUid, Is.EqualTo(centcommMap));

        // Set up shuttle timing
        var evacSys = server.System<EmergencyShuttleSystem>();
        evacSys.TransitTime = ShuttleSystem.DefaultTravelTime; // Absolute minimum transit time, so the test has to run for at least this long
        // TODO SHUTTLE fix spaghetti

        var dockTime = server.CfgMan.GetCVar(CCVars.EmergencyShuttleDockTime);
        server.CfgMan.SetCVar(CCVars.EmergencyShuttleDockTime, 2);
        async Task RunSeconds(float seconds)
        {
            await pair.RunTicksSync((int) Math.Ceiling(seconds / server.Timing.TickPeriod.TotalSeconds));
        }

        // Call evac shuttle.
        await pair.WaitCommand("callshuttle 0:02");
        await RunSeconds(3);

        // Shuttle should have arrived on the station
        Assert.That(shuttleXform.MapUid, Is.EqualTo(salternXform.MapUid));

        await RunSeconds(2);

        // Shuttle should be FTLing back to centcomm
        Assert.That(entMan.Count<FTLMapComponent>(), Is.EqualTo(1));
        var ftl = (Entity<FTLMapComponent>) entMan.AllComponentsList<FTLMapComponent>().Single();
        Assert.That(entMan.HasComponent<MapComponent>(ftl));
        Assert.That(ftl.Owner, Is.Not.EqualTo(centcommMap));
        Assert.That(ftl.Owner, Is.Not.EqualTo(salternXform.MapUid));
        Assert.That(shuttleXform.MapUid, Is.EqualTo(ftl.Owner));

        // Shuttle should have arrived at centcomm
        await RunSeconds(ShuttleSystem.DefaultTravelTime);
        Assert.That(shuttleXform.MapUid, Is.EqualTo(centcommMap));

        // Round should be ending now
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));

        server.CfgMan.SetCVar(CCVars.EmergencyShuttleDockTime, dockTime);
        pair.Server.CfgMan.SetCVar(CCVars.EmergencyShuttleEnabled, shuttleEnabled);
        await pair.CleanReturnAsync();
    }
}

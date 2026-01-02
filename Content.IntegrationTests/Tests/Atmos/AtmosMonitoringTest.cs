using System.Numerics;
using Content.Server.Atmos.Monitor.Components;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Test for determining that an AtmosMonitoringComponent/System correctly references
/// the GasMixture of the tile it is on if the tile's GasMixture ever changes.
/// </summary>
[TestOf(typeof(Atmospherics))]
public sealed class AtmosMonitoringTest : AtmosTest
{
    // We can just reuse the dP test, I just want a grid.
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    private readonly EntProtoId _airSensorProto = new("AirSensor");
    private readonly EntProtoId _wallProto = new("WallSolid");

    /// <summary>
    /// Tests if the monitor properly nulls out its reference to the tile mixture
    /// when a wall is placed on top of it, and restores the reference when the wall is removed.
    /// </summary>
    [Test]
    public async Task NullOutTileAtmosphereGasMixture()
    {
        // run an atmos update to initialize everything For Real surely
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        var gridNetEnt = SEntMan.GetNetEntity(RelevantAtmos.Owner);
        TargetCoords = new NetCoordinates(gridNetEnt, Vector2.Zero);
        var netEnt = await Spawn(_airSensorProto);
        var airSensorUid = SEntMan.GetEntity(netEnt);
        Transform.TryGetGridTilePosition(airSensorUid, out var vec);

        // run another one to ensure that the ref to the GasMixture was picked up
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // should be in the middle
        Assert.That(vec,
            Is.EqualTo(Vector2i.Zero),
            "Air sensor not in expected position on grid (0, 0)");

        var atmosMonitor = SEntMan.GetComponent<AtmosMonitorComponent>(airSensorUid);
        var tileMixture = SAtmos.GetTileMixture(airSensorUid);

        Assert.That(tileMixture,
            Is.SameAs(atmosMonitor.TileGas),
            "Atmos monitor's TileGas does not match actual tile mixture after spawn.");

        // ok now spawn a wall or something on top of it
        var wall = await Spawn(_wallProto);
        var wallUid = SEntMan.GetEntity(wall);

        // ensure that atmospherics registers the change - the gas mixture should no longer exist
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // the monitor's ref to the gas should be null now
        Assert.That(atmosMonitor.TileGas,
            Is.Null,
            "Atmos monitor's TileGas is not null after wall placed on top. Possible dead reference.");
        // the actual mixture on the tile should be null now too
        var nullTileMixture = SAtmos.GetTileMixture(airSensorUid);
        Assert.That(nullTileMixture, Is.Null, "Tile mixture is not null after wall placed on top.");

        // ok now delete the wall
        await Delete(wallUid);

        // ensure that atmospherics registers the change - the gas mixture should be back
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // gas mixture should now exist again
        var newTileMixture = SAtmos.GetTileMixture(airSensorUid);
        Assert.That(newTileMixture, Is.Not.Null, "Tile mixture is null after wall removed.");
        // monitor's ref to the gas should be back too
        Assert.That(atmosMonitor.TileGas,
            Is.SameAs(newTileMixture),
            "Atmos monitor's TileGas does not match actual tile mixture after wall removed.");
    }

    /// <summary>
    /// Tests if the monitor properly updates its reference to the tile mixture
    /// when the FixGridAtmos command is called.
    /// </summary>
    [Test]
    public async Task FixGridAtmosReplaceMixtureOnTileChange()
    {
        // run an atmos update to initialize everything For Real surely
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        var gridNetEnt = SEntMan.GetNetEntity(RelevantAtmos.Owner);
        TargetCoords = new NetCoordinates(gridNetEnt, Vector2.Zero);
        var netEnt = await Spawn(_airSensorProto);
        var airSensorUid = SEntMan.GetEntity(netEnt);
        Transform.TryGetGridTilePosition(airSensorUid, out var vec);

        // run another one to ensure that the ref to the GasMixture was picked up
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // should be in the middle
        Assert.That(vec,
            Is.EqualTo(Vector2i.Zero),
            "Air sensor not in expected position on grid (0, 0)");

        var atmosMonitor = SEntMan.GetComponent<AtmosMonitorComponent>(airSensorUid);
        var tileMixture = SAtmos.GetTileMixture(airSensorUid);

        Assert.That(tileMixture,
            Is.SameAs(atmosMonitor.TileGas),
            "Atmos monitor's TileGas does not match actual tile mixture after spawn.");

        SAtmos.RebuildGridAtmosphere((ProcessEnt.Owner, ProcessEnt.Comp1, ProcessEnt.Comp3));

        // EXTREMELY IMPORTANT: The reference to the tile mixture on the tile should be completely different.
        var newTileMixture = SAtmos.GetTileMixture(airSensorUid);
        Assert.That(newTileMixture,
            Is.Not.SameAs(tileMixture),
            "Tile mixture is the same instance after fixgridatmos was ran. It should be a new instance.");

        // The monitor's ref to the tile mixture should have updated too.
        Assert.That(atmosMonitor.TileGas,
            Is.SameAs(newTileMixture),
            "Atmos monitor's TileGas does not match actual tile mixture after fixgridatmos was ran.");
    }
}

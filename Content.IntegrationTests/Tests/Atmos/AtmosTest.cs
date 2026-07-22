using System.Diagnostics.CodeAnalysis;
using Content.Client.Atmos.EntitySystems;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
using Content.Shared.Tests;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Base class for tests involving <see cref="AtmosphereSystem"/>.
/// Primarily in charge of setting up common dependencies and providing a test map if needed.
/// See <see cref="TileAtmosphereTest"/> on how to add new tests with custom maps.
/// </summary>
public abstract partial class AtmosTest : GameTest
{
    [SidedDependency(Side.Server)] protected Server.Atmos.EntitySystems.AtmosphereSystem SAtmos = default!;
    [SidedDependency(Side.Client)] protected Client.Atmos.EntitySystems.AtmosphereSystem CAtmos = default!;

    /// <summary>
    /// Used in <see cref="AtmosphereSystem.RunProcessingFull"/>. Resolved during test setup.
    /// </summary>
    /// <remarks>This will be null if no <see cref="TestMapPath"/> is provided.</remarks>
    protected Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ProcessEnt;

    /// <summary>
    /// Helper shorthand of <see cref="ProcessEnt"/>.
    /// </summary>
    /// <remarks>This will be null if no <see cref="TestMapPath"/> is provided.</remarks>
    protected Entity<GridAtmosphereComponent> RelevantAtmos => (ProcessEnt.Owner, ProcessEnt.Comp1);

    /// <summary>
    /// Map to automatically load for tests. Override if you want the helper to automatically
    /// load the provided map and resolve the <see cref="ProcessEnt"/> for you.
    /// If null, no map is loaded.
    /// </summary>
    /// <remarks>This map will have necessary Atmospherics components EnsureComp'd onto it.</remarks>
    protected virtual ResPath? TestMapPath => null;

    protected TestMapData MapData => Pair.TestMap;

    protected virtual float Moles => 1000.0f;

    // 5% is a lot, but it can get this bad ATM...
    protected virtual float Tolerance => 0.05f;

    [SetUp]
    public virtual async Task Setup()
    {
        if (!TestMapPathNotNull())
            return;

        Pair.TestMap = await Pair.LoadTestMap(TestMapPath.Value);

        var gridAtmosComp = SComp<GridAtmosphereComponent>(MapData.Grid);
        var overlayComp = SComp<GasTileOverlayComponent>(MapData.Grid);
        var mapGridComp = SComp<MapGridComponent>(MapData.Grid);
        var xform = SComp<TransformComponent>(MapData.Grid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gridAtmosComp,
                Is.Not.Null,
                "Loaded map doesn't have a GridAtmosphereComponent on its grid. " +
                "Does your TestMapPath have the necessary Atmospherics components?");
            Assert.That(overlayComp,
                Is.Not.Null,
                "Loaded map doesn't have a GasTileOverlayComponent on its grid. " +
                "Does your TestMapPath have the necessary Atmospherics components?");
            Assert.That(mapGridComp,
                Is.Not.Null,
                "Loaded map doesn't have a MapGridComponent on its grid. " +
                "Does your TestMapPath have the necessary Atmospherics components?");
        }

        ProcessEnt = new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(
            MapData.Grid.Owner,
            gridAtmosComp,
            overlayComp,
            mapGridComp,
            xform);

        await Server.WaitPost(() =>
        {
            var cfg = Server.ResolveDependency<IConfigurationManager>();

            //Make sure Atmos is always processed fully during tick and not split up
            cfg.SetCVar(CCVars.AtmosMaxProcessTime, 9999f);
        });

        // Make sure the map and whatnot is fleshed out.
        await RunUntilSynced();
    }

    [MemberNotNullWhen(true,
        nameof(ProcessEnt),
        nameof(RelevantAtmos),
        nameof(TestMapPath))]
    private bool TestMapPathNotNull()
    {
        // nullability analysis GO
        return TestMapPath != null;
    }

    /// <summary>
    /// Tries to get a mapped <see cref="TestMarkerComponent"/> marker with a given name.
    /// </summary>
    /// <param name="markers">Marker entities to look through</param>
    /// <param name="id">Marker name to look up (set during mapping)</param>
    /// <param name="marker">Found marker EntityUid or Invalid</param>
    /// <returns>True if found</returns>
    protected static bool GetMarker(Entity<TestMarkerComponent>[] markers, string id, out EntityUid marker)
    {
        foreach (var ent in markers)
        {
            if (ent.Comp.Id == id)
            {
                marker = ent;
                return true;
            }
        }
        marker = EntityUid.Invalid;
        return false;
    }

    protected static float GetGridMoles(Entity<GridAtmosphereComponent> grid)
    {
        var moles = 0.0f;
        foreach (var tile in grid.Comp.Tiles.Values)
        {
            moles += tile.Air?.TotalMoles ?? 0.0f;
        }

        return moles;
    }

    /// <summary>
    /// Asserts that test grid has this many moles, within tolerance percentage.
    /// </summary>
    protected void AssertGridMoles(float moles, float tolerance)
    {
        var gridMoles = GetGridMoles(RelevantAtmos);
        Assert.That(MathHelper.CloseToPercent(moles, gridMoles, tolerance), $"Grid has {gridMoles} moles, but {moles} was expected");
    }

    /// <summary>
    /// Asserts that provided GasMixtures have same total moles, within tolerance percentage.
    /// </summary>
    protected void AssertMixMoles(GasMixture mix1, GasMixture mix2, float tolerance)
    {
        Assert.That(MathHelper.CloseToPercent(mix1.TotalMoles, mix2.TotalMoles, tolerance),
            $"GasMixtures do not match. Got {mix1.TotalMoles} and {mix2.TotalMoles} moles");
    }

    /// <summary>
    /// Sets the tile's air mixture to have a certain pressure at a certain temperature.
    /// </summary>
    /// <param name="tile">Tile to set the air mixture of.</param>
    /// <param name="pressure">The pressure to set the tile to.</param>
    /// <param name="temp">The temperature to set the tile to.</param>
    /// <param name="gas">The gas to fill the tile with.</param>
    /// <remarks>Yeah, it could be a general atmospherics API, but the test assertion is desired.</remarks>
    protected static void SetTilePressure(TileAtmosphere tile, float pressure, float temp = Atmospherics.T20C, Gas gas = Gas.Nitrogen)
    {
        Assert.That(tile.Air, Is.Not.Null, "Target tile should have an air mixture.");
        tile.Air!.Clear();
        var moles = pressure * tile.Air.Volume / (Atmospherics.R * temp);
        tile.Air.AdjustMoles(gas, moles);
    }
}

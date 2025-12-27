using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Helper class for atmospherics tests.
/// See <see cref="TileAtmosphereTest"/> on how to add new tests with custom maps.
/// </summary>
[TestFixture]
public abstract class AtmosTest : InteractionTest
{
    protected AtmosphereSystem SAtmos = default!;
    protected EntityLookupSystem LookupSystem = default!;

    protected Entity<GridAtmosphereComponent> RelevantAtmos = default!;

    /// <summary>
    /// Used in <see cref="AtmosphereSystem.RunProcessingFull"/>. Resolved during test setup.
    /// </summary>
    protected Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ProcessEnt = default;

    protected virtual float Moles => 1000.0f;

    // 5% is a lot, but it can get this bad ATM...
    protected virtual float Tolerance => 0.05f;

    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();

        SAtmos = SEntMan.System<AtmosphereSystem>();
        LookupSystem = SEntMan.System<EntityLookupSystem>();

        RelevantAtmos = (MapData.Grid, SEntMan.GetComponent<GridAtmosphereComponent>(MapData.Grid));

        ProcessEnt = new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(
            MapData.Grid.Owner,
            SEntMan.GetComponent<GridAtmosphereComponent>(MapData.Grid.Owner),
            SEntMan.GetComponent<GasTileOverlayComponent>(MapData.Grid.Owner),
            SEntMan.GetComponent<MapGridComponent>(MapData.Grid.Owner),
            SEntMan.GetComponent<TransformComponent>(MapData.Grid.Owner));
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
}

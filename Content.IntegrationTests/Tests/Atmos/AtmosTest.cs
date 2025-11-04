using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
///
/// </summary>
public abstract class AtmosTest : InteractionTest
{
    protected AtmosphereSystem SAtmos = default!;
    protected EntityLookupSystem LookupSystem = default!;

    protected Entity<GridAtmosphereComponent> RelevantAtmos = default!;

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
    }

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

}

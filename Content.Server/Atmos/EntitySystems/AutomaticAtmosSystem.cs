using Content.Server.Atmos.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Handles automatically adding a grid atmosphere to grids that become large enough, allowing players to build shuttles
/// with a sealed atmosphere from scratch.
/// </summary>
public sealed class AutomaticAtmosSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        // Only if a atmos-holding tile has been added or removed.
        // Also, these calls are surprisingly slow.
        // TODO: Make tiledefmanager cache the IsSpace property, and turn this lookup-through-two-interfaces into
        // TODO: a simple array lookup, as tile IDs are likely contiguous, and there's at most 2^16 possibilities anyway.

        var oldSpace = ev.OldTile.IsSpace(_tileDefinitionManager);
        var newSpace = ev.NewTile.IsSpace(_tileDefinitionManager);

        if (!(oldSpace && !newSpace ||
            !oldSpace && newSpace) ||
            _atmosphereSystem.HasAtmosphere(ev.Entity))
            return;

        if (!TryComp<PhysicsComponent>(ev.Entity, out var physics))
            return;

        // We can't actually count how many tiles there are efficiently, so instead estimate with the mass.
        if (physics.Mass / ShuttleSystem.TileMassMultiplier >= 7.0f)
        {
            AddComp<GridAtmosphereComponent>(ev.Entity);
            Log.Info($"Giving grid {ev.Entity} GridAtmosphereComponent.");
        }
        // It's not super important to remove it should the grid become too small again.
        // If explosions ever gain the ability to outright shatter grids, do rethink this.
    }
}

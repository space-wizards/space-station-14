using Content.Server.Atmos.Components;
using Content.Server.Shuttles.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Handles automatically adding a grid atmosphere to grids that become large enough, allowing players to build shuttles
/// with a sealed atmosphere from scratch.
/// </summary>
public sealed class AutomaticAtmosSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _mapManager.TileChanged += MapManagerOnTileChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _mapManager.TileChanged -= MapManagerOnTileChanged;
    }

    private void MapManagerOnTileChanged(object? sender, TileChangedEventArgs e)
    {
        // Only if a atmos-holding tile has been added or removed.
        // Also, these calls are surprisingly slow.
        // TODO: Make tiledefmanager cache the IsSpace property, and turn this lookup-through-two-interfaces into
        // TODO: a simple array lookup, as tile IDs are likely contiguous, and there's at most 2^16 possibilities anyway.
        if (!((e.OldTile.IsSpace(_tileDefinitionManager) && !e.NewTile.IsSpace(_tileDefinitionManager)) ||
            (!e.OldTile.IsSpace(_tileDefinitionManager) && e.NewTile.IsSpace(_tileDefinitionManager))))
            return;

        var uid = _mapManager.GetGridEuid(e.NewTile.GridIndex);
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        // We can't actually count how many tiles there are efficiently, so instead estimate with the mass.
        if (physics.Mass / ShuttleSystem.TileMassMultiplier >= 7.0f && !HasComp<IAtmosphereComponent>(uid))
        {
            AddComp<GridAtmosphereComponent>(uid);
            Logger.InfoS("atmos", $"Giving grid {uid} GridAtmosphereComponent.");
        }
        // It's not super important to remove it should the grid become too small again.
        // If explosions ever gain the ability to outright shatter grids, do rethink this.
    }
}

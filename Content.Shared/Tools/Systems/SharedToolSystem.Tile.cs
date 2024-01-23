using Content.Shared.Database;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Systems;

public abstract partial class SharedToolSystem
{
    [Dependency] private readonly INetManager _net = default!;

    public void InitializeTile()
    {
        SubscribeLocalEvent<ToolTileCompatibleComponent, AfterInteractEvent>(OnToolTileAfterInteract);
        SubscribeLocalEvent<ToolTileCompatibleComponent, TileToolDoAfterEvent>(OnToolTileComplete);
    }

    private void OnToolTileAfterInteract(Entity<ToolTileCompatibleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target != null && !HasComp<PuddleComponent>(args.Target))
            return;

        args.Handled = UseToolOnTile((ent, ent, null), args.User, args.ClickLocation);
    }

    private void OnToolTileComplete(Entity<ToolTileCompatibleComponent> ent, ref TileToolDoAfterEvent args)
    {
        var comp = ent.Comp;
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<ToolComponent>(ent, out var tool))
            return;
        var coordinates = GetCoordinates(args.Coordinates);

        var gridUid = coordinates.GetGridUid(EntityManager);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Log.Error("Attempted use tool on a non-existent grid?");
            return;
        }

        var tileRef = _maps.GetTileRef(gridUid.Value, grid, coordinates);
        if (comp.RequiresUnobstructed && _turfs.IsTileBlocked(gridUid.Value, tileRef.GridIndices, CollisionGroup.MobMask))
            return;

        if (!TryDeconstructWithToolQualities(tileRef, tool.Qualities))
            return;

        AdminLogger.Add(LogType.LatticeCut, LogImpact.Medium,
                $"{ToPrettyString(args.User):player} used {ToPrettyString(ent)} to edit the tile at {args.Coordinates}");
        args.Handled = true;
    }

    private bool UseToolOnTile(Entity<ToolTileCompatibleComponent?, ToolComponent?> ent, EntityUid user, EntityCoordinates clickLocation)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return false;

        var comp = ent.Comp1!;
        var tool = ent.Comp2!;

        if (!_mapManager.TryFindGridAt(clickLocation.ToMap(EntityManager, _transformSystem), out var gridUid, out var mapGrid))
            return false;

        var tileRef = _maps.GetTileRef(gridUid, mapGrid, clickLocation);
        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];

        if (!tool.Qualities.ContainsAny(tileDef.DeconstructTools))
            return false;

        if (string.IsNullOrWhiteSpace(tileDef.BaseTurf))
            return false;

        if (comp.RequiresUnobstructed && _turfs.IsTileBlocked(gridUid, tileRef.GridIndices, CollisionGroup.MobMask))
            return false;

        var coordinates = _maps.GridTileToLocal(gridUid, mapGrid, tileRef.GridIndices);
        if (!InteractionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        var args = new TileToolDoAfterEvent(GetNetCoordinates(coordinates));
        UseTool(ent, user, ent, comp.Delay, tool.Qualities, args, out _, toolComponent: tool);
        return true;
    }

    public bool TryDeconstructWithToolQualities(TileRef tileRef, PrototypeFlags<ToolQualityPrototype> withToolQualities)
    {
        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];
        if (withToolQualities.ContainsAny(tileDef.DeconstructTools))
        {
            // don't do this on the client or else the tile entity spawn mispredicts and looks horrible
            return _net.IsClient || _tiles.DeconstructTile(tileRef);
        }
        return false;
    }
}

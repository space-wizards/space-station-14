using Content.Server.Atmos.Components;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Atmos;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.DeviceLinking.Systems;

public sealed class AlarmAutoLinkSystem : EntitySystem
{
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlarmAutoLinkComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AlarmAutoLinkComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceListComponent>(ent, out var list))
            return;

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp(gridUid, out MapGridComponent? gridComp))
            return;

        var grid = new Entity<MapGridComponent>(gridUid, gridComp);
        var indices = _mapSystem.TileIndicesFor(grid, xform.Coordinates);
        // We indent one tile in the direction of rotation of the entity
        var seed = (indices + xform.LocalRotation.ToWorldVec()).Floored();

        var queue = new Queue<TileRef>();
        var visited = new HashSet<Vector2i>();

        queue.Enqueue(_mapSystem.GetTileRef(grid, seed));

        var directions = new Vector2i[]
        {
            new(0, 1), new(0, -1),
            new (1, 0), new(-1, 0),
        };

        // Using flood-fill to iterate room tiles
        while (queue.TryDequeue(out var node))
        {
            if (!visited.Add(node.GridIndices))
                continue;

            var isBorder = false;
            var worldCoord = _mapSystem.LocalToWorld(grid.Owner, grid.Comp, node.GridIndices);
            var worldAABB = new Box2(worldCoord, worldCoord + grid.Comp.TileSizeVector).Enlarged(-0.1f);
            var entities = _entityLookupSystem.GetEntitiesIntersecting(gridUid, worldAABB);

            foreach (var entity in entities)
            {
                if (MetaData(entity).EntityPrototype is not { } proto)
                    continue;

                // Using AirtightComponent to detect room borders
                if (!isBorder && TryComp(entity, out AirtightComponent? airtight))
                {
                    if (airtight.AirBlocked)
                    {
                        if (((AtmosDirection) airtight.CurrentAirBlockedDirection).HasFlag(AtmosDirection.All))
                            isBorder = true;
                    }
                    // Firelocks are always considered as room borders
                    else if (TryComp(entity, out FirelockComponent? _))
                    {
                        isBorder = true;
                    }
                }

                if (ent.Comp.AutoLinkPrototypes.Contains(proto))
                    _deviceListSystem.UpdateDeviceList(ent, new List<EntityUid> { entity }, true, list);
            }

            if (isBorder)
                continue;

            foreach (var offset in directions)
            {
                var nextTile = _mapSystem.GetTileRef(grid, node.GridIndices + offset);
                if (!nextTile.IsSpace())
                    queue.Enqueue(nextTile);
            }
        }
    }
}

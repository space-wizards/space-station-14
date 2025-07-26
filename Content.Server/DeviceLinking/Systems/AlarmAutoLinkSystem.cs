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

        var queue = new Queue<(TileRef, AtmosDirection)>();
        var visited = new HashSet<(Vector2i, AtmosDirection)>();

        queue.Enqueue((_mapSystem.GetTileRef(grid, seed), xform.LocalRotation.ToAtmosDirectionCardinal()));

        var directions = new HashSet<(Vector2i, AtmosDirection)>
        {
            (Vector2i.Up, AtmosDirection.North),
            (Vector2i.Down, AtmosDirection.South),
            (Vector2i.Left, AtmosDirection.West),
            (Vector2i.Right, AtmosDirection.East),
        };

        // Using flood-fill to iterate room tiles
        while (queue.TryDequeue(out var node))
        {
            var (tileRef, direction) = node;
            if (!visited.Add((tileRef.GridIndices, direction)))
                continue;

            var blockedDirections = AtmosDirection.Invalid;
            var worldCoord = _mapSystem.LocalToWorld(grid.Owner, grid.Comp, tileRef.GridIndices);
            var worldAABB = new Box2(worldCoord, worldCoord + grid.Comp.TileSizeVector).Enlarged(-0.05f);
            var entities = _entityLookupSystem.GetEntitiesIntersecting(gridUid, worldAABB, LookupFlags.StaticSundries);

            foreach (var entity in entities)
            {
                // Using AirtightComponent to detect room borders
                if (TryComp(entity, out AirtightComponent? airtight)
                    && (airtight.AirBlocked || TryComp(entity, out FirelockComponent? _)))
                {
                    blockedDirections = blockedDirections.WithFlag(airtight.AirBlockedDirection);
                }

                if (MetaData(entity).EntityPrototype is not { } proto)
                    continue;

                if (ent.Comp.AutoLinkPrototypes.Contains(proto))
                    _deviceListSystem.UpdateDeviceList(ent, new List<EntityUid> { entity }, true, list);
            }

            // Skip if the direction from which the check is performed is blocked
            if (blockedDirections.HasFlag(AtmosDirection.All)
                || blockedDirections.HasFlag(direction.GetOpposite()))
                continue;

            // Iterate 4 neighboring tiles if they are not blocked
            foreach (var (offset, offsetDir) in directions)
            {
                if (blockedDirections.HasFlag(offsetDir))
                    continue;

                var nextTile = _mapSystem.GetTileRef(grid, tileRef.GridIndices + offset);
                if (!nextTile.IsSpace())
                    queue.Enqueue((nextTile, offsetDir));
            }
        }
    }
}

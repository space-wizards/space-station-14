using System.Numerics;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Pinpointer;

public sealed class NavMapSystem : SharedNavMapSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NavMapComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, NavMapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NavMapComponentState state)
            return;

        component.Chunks.Clear();

        foreach (var (origin, data) in state.TileData)
        {
            component.Chunks.Add(origin, new NavMapChunk(origin)
            {
                TileData = data,
            });
        }

        component.Beacons.Clear();
        component.Beacons.AddRange(state.Beacons);

        component.Airlocks.Clear();
        component.Airlocks.AddRange(state.Airlocks);
    }
}

public sealed class NavMapOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private List<Entity<MapGridComponent>> _grids = new();

    public NavMapOverlay(IEntityManager entManager, IMapManager mapManager)
    {
        _entManager = entManager;
        _mapManager = mapManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.GetEntityQuery<NavMapComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var scale = Matrix3.CreateScale(new Vector2(1f, 1f));

        _grids.Clear();
        _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds, ref _grids);

        foreach (var grid in _grids)
        {
            if (!query.TryGetComponent(grid, out var navMap) || !xformQuery.TryGetComponent(grid.Owner, out var xform))
                continue;

            // TODO: Faster helper method
            var (_, _, matrix, invMatrix) = xform.GetWorldPositionRotationMatrixWithInv();

            var localAABB = invMatrix.TransformBox(args.WorldBounds);
            Matrix3.Multiply(in scale, in matrix, out var matty);

            args.WorldHandle.SetTransform(matty);

            for (var x = Math.Floor(localAABB.Left); x <= Math.Ceiling(localAABB.Right); x += SharedNavMapSystem.ChunkSize * grid.Comp.TileSize)
            {
                for (var y = Math.Floor(localAABB.Bottom); y <= Math.Ceiling(localAABB.Top); y += SharedNavMapSystem.ChunkSize * grid.Comp.TileSize)
                {
                    var floored = new Vector2i((int) x, (int) y);

                    var chunkOrigin = SharedMapSystem.GetChunkIndices(floored, SharedNavMapSystem.ChunkSize);

                    if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
                        continue;

                    // TODO: Okay maybe I should just use ushorts lmao...
                    for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
                    {
                        var value = (int) Math.Pow(2, i);

                        var mask = chunk.TileData & value;

                        if (mask == 0x0)
                            continue;

                        var tile = chunk.Origin * SharedNavMapSystem.ChunkSize + SharedNavMapSystem.GetTile(mask);
                        args.WorldHandle.DrawRect(new Box2(tile * grid.Comp.TileSize, (tile + 1) * grid.Comp.TileSize), Color.Aqua, false);
                    }
                }
            }
        }

        args.WorldHandle.SetTransform(Matrix3.Identity);
    }
}

using System.Numerics;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Light;

public sealed class TileEmissionOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IMapManager _mapManager = default!;

    private SharedMapSystem _mapSystem;
    private SharedTransformSystem _xformSystem;

    private readonly EntityLookupSystem _lookup;

    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly HashSet<Entity<TileEmissionComponent>> _entities = new();

    private List<Entity<MapGridComponent>> _grids = new();

    public const int ContentZIndex = RoofOverlay.ContentZIndex + 1;

    public TileEmissionOverlay(IEntityManager entManager)
    {
        IoCManager.InjectDependencies(this);

        _lookup = entManager.System<EntityLookupSystem>();
        _mapSystem = entManager.System<SharedMapSystem>();
        _xformSystem = entManager.System<SharedTransformSystem>();

        _xformQuery = entManager.GetEntityQuery<TransformComponent>();
        ZIndex = ContentZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var mapId = args.MapId;
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var expandedBounds = bounds.Enlarged(1.5f);
        var viewport = args.Viewport;

        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
        () =>
        {
            var invMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale / 2f);
            _grids.Clear();
            _mapManager.FindGridsIntersecting(mapId, expandedBounds, ref _grids, approx: true);

            foreach (var grid in _grids)
            {
                var gridInvMatrix = _xformSystem.GetInvWorldMatrix(grid);
                var localBounds = gridInvMatrix.TransformBox(expandedBounds);
                _entities.Clear();
                _lookup.GetLocalEntitiesIntersecting(grid.Owner, localBounds, _entities);

                if (_entities.Count == 0)
                    continue;

                var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);

                foreach (var ent in _entities)
                {
                    var xform = _xformQuery.Comp(ent);

                    var tile = _mapSystem.LocalToTile(grid.Owner, grid, xform.Coordinates);
                    var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                    worldHandle.SetTransform(matty);

                    // Yes I am fully aware this leads to overlap. If you really want to have alpha then you'll need
                    // to turn the squares into polys.
                    // Additionally no shadows so if you make it too big it's going to go through a 1x wall.
                    var local = _lookup.GetLocalBounds(tile, grid.Comp.TileSize).Enlarged(ent.Comp.Range);
                    worldHandle.DrawRect(local, ent.Comp.Color);
                }
            }
        }, null);
    }
}

using System.Numerics;
using Content.Client.Light.Components;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Client.Light;

public sealed class TileEmissionOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private SharedMapSystem _mapSystem;
    private SharedTransformSystem _xformSystem;

    private readonly EntityLookupSystem _lookup;

    private readonly EntityQuery<TransformComponent> _xformQuery;

    private List<Entity<MapGridComponent>> _grids = new();

    public const int ContentZIndex = RoofOverlay.ContentZIndex + 1;

    private List<ComponentTreeEntry<TileEmissionComponent>> _emissions = new();

    public TileEmissionOverlay(IEntityManager entManager)
    {
        IoCManager.InjectDependencies(this);

        _entManager = entManager;
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
        var lightoverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var bounds = lightoverlay.EnlargedBounds;
        var target = lightoverlay.GetCachedForViewport(args.Viewport).EnlargedLightTarget;
        var viewport = args.Viewport;
        _grids.Clear();
        _mapManager.FindGridsIntersecting(mapId, bounds, ref _grids, approx: true);

        // Cull any unnecessary grids
        for (var i = _grids.Count - 1; i >= 0; i--)
        {
            var grid = _grids[i];

            if (!_entManager.TryGetComponent(grid.Owner, out TileEmissionTreeComponent? tree) ||
                tree.Tree.Count == 0)
            {
                _grids.RemoveAt(i);
            }
        }

        if (_grids.Count == 0)
            return;

        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);

        args.WorldHandle.RenderInRenderTarget(target,
        () =>
        {
            var invMatrix = target.GetWorldToLocalMatrix(viewport.Eye, scale);

            foreach (var grid in _grids)
            {
                _emissions.Clear();
                var tree = _entManager.GetComponent<TileEmissionTreeComponent>(grid.Owner);

                var gridInvMatrix = _xformSystem.GetInvWorldMatrix(grid);
                var localBounds = gridInvMatrix.TransformBox(bounds);
                var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);
                var state = (_emissions);

                tree.Tree.QueryAabb(ref state,
                    (ref List<ComponentTreeEntry<TileEmissionComponent>> list,
                        in ComponentTreeEntry<TileEmissionComponent> value) =>
                    {
                        list.Add(value);
                        return true;
                    }, aabb: localBounds, approx: true);

                foreach (var ent in _emissions)
                {
                    var tile = _mapSystem.LocalToTile(grid.Owner, grid, ent.Transform.Coordinates);
                    var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                    worldHandle.SetTransform(matty);

                    // Yes I am fully aware this leads to overlap. If you really want to have alpha then you'll need
                    // to turn the squares into polys.
                    // Additionally no shadows so if you make it too big it's going to go through a 1x wall.
                    var local = _lookup.GetLocalBounds(tile, grid.Comp.TileSize).Enlarged(ent.Component.Range);
                    worldHandle.DrawRect(local, ent.Component.Color);
                }
            }
        }, null);
    }
}

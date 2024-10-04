using System.Numerics;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.Light;

public sealed class TileEmissionOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IClyde _clyde = default!;

    private readonly IEntityManager _entManager;
    private readonly EntityLookupSystem _lookup;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private readonly HashSet<Entity<TileEmissionComponent>> _entities = new();

    public TileEmissionOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        IoCManager.InjectDependencies(this);

        _lookup = _entManager.System<EntityLookupSystem>();
        _gridQuery = _entManager.GetEntityQuery<MapGridComponent>();
        _xformQuery = _entManager.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var mapId = args.MapId;
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var viewport = args.Viewport;

        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
        () =>
        {
            var invMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale / 2f);
            _entities.Clear();
            _lookup.GetEntitiesIntersecting(mapId, bounds, _entities);

            foreach (var ent in _entities)
            {
                var xform = _xformQuery.Comp(ent);

                if (!_gridQuery.TryComp(xform.GridUid, out var grid))
                    continue;

                // TODO: Optimise, allocate ents to each grid for transforms
                var tile = _entManager.System<SharedMapSystem>().LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);

                var gridMatrix = _entManager.System<SharedTransformSystem>().GetWorldMatrix(xform.GridUid.Value);
                var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                worldHandle.SetTransform(matty);

                // Yes I am fully aware this leads to overlap. If you really want to have alpha then you'll need
                // to turn the squares into polys.
                // Additionally no shadows so if you make it too big it's going to go through a 1x wall.
                var local = _lookup.GetLocalBounds(tile, grid.TileSize).Enlarged(ent.Comp.Range);
                worldHandle.DrawRect(local, ent.Comp.Color);
            }
        }, null);

        // This also handles blurring for roofoverlay; if these ever become decoupled then you will need to draw at least
        // one of these to a separate texture.
        _clyde.BlurLights(viewport, viewport.LightRenderTarget, viewport.Eye, 14f * 4f);
    }
}

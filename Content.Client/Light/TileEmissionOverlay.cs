using System.Numerics;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed class TileEmissionOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private readonly IEntityManager _entManager;
    private readonly EntityLookupSystem _lookup;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private readonly HashSet<Entity<TileEmissionComponent>> _entities = new();

    private IRenderTexture? _target;

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
        var expandedBounds = bounds.Enlarged(1.5f);
        var viewport = args.Viewport;

        if (_target?.Size != viewport.LightRenderTarget.Size)
        {
            _target = _clyde
                .CreateRenderTarget(viewport.LightRenderTarget.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "tile-emissions");
        }

        args.WorldHandle.RenderInRenderTarget(_target,
        () =>
        {
            var invMatrix = _target.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale / 2f);
            _entities.Clear();
            _lookup.GetEntitiesIntersecting(mapId, expandedBounds, _entities);

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
        }, Color.Transparent);

        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                var invMatrix =
                    viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale / 2f);
                worldHandle.SetTransform(invMatrix);

                var maskShader = _protoManager.Index<ShaderPrototype>("Mix").Instance();
                worldHandle.UseShader(maskShader);

                worldHandle.DrawTextureRect(_target.Texture, bounds);
            }, null);
    }
}

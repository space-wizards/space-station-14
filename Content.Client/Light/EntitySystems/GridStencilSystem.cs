using System.Numerics;
using System.Runtime.InteropServices;
using Content.Client.Graphics;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Light.EntitySystems;

/// <summary>
/// Builds viewport-sized masks for grid contents that need to be reused by world overlays.
/// </summary>
public sealed partial class GridStencilSystem : EntitySystem
{
    public delegate bool TileStencilPredicate(Entity<MapGridComponent> grid, TileRef tile);

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IMapManager _mapManager = default!;

    private EntityLookupSystem _lookup = default!;
    private SharedMapSystem _map = default!;
    private SharedTransformSystem _xform = default!;
    private TurfSystem _turf = default!;

    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private List<Entity<MapGridComponent>> _grids = new();
    private readonly List<WorldRect> _rects = new();
    private readonly List<Box2> _tileBounds = new();

    public override void Initialize()
    {
        base.Initialize();

        _lookup = EntityManager.System<EntityLookupSystem>();
        _map = EntityManager.System<SharedMapSystem>();
        _xform = EntityManager.System<SharedTransformSystem>();
        _turf = EntityManager.System<TurfSystem>();
    }

    /// <summary>
    /// Returns a viewport-sized texture where non-space grid tiles are white and everything else is transparent.
    /// The texture is rebuilt at most once per viewport per frame.
    /// </summary>
    public IRenderTexture GetNonSpaceStencil(in OverlayDrawArgs args)
    {
        return GetTileBoundsStencil(args,
            "non-space",
            "non-space-grid-stencil",
            tile => !_turf.IsSpace(tile));
    }

    private IRenderTexture GetTileBoundsStencil(
        in OverlayDrawArgs args,
        string key,
        string name,
        Predicate<Tile> predicate)
    {
        var viewport = args.Viewport;
        var target = viewport.RenderTarget;
        var res = _resources.GetForViewport(viewport, static _ => new CachedResources());
        var targetRes = res.GetOrCreate(key);

        if (targetRes.Target?.Texture.Size != target.Size)
        {
            targetRes.Target?.Dispose();
            targetRes.Target = _clyde.CreateRenderTarget(target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: name);
            targetRes.LastFrame = 0;
        }

        var worldHandle = args.WorldHandle;
        var invMatrix = viewport.GetWorldToLocalMatrix();
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;

        if (targetRes.LastFrame == _timing.CurFrame &&
            targetRes.LastMapId == mapId &&
            targetRes.LastWorldBounds.Equals(worldBounds))
        {
            return targetRes.Target!;
        }

        targetRes.LastFrame = _timing.CurFrame;
        targetRes.LastMapId = mapId;
        targetRes.LastWorldBounds = worldBounds;

        worldHandle.RenderInRenderTarget(targetRes.Target,
            () =>
            {
                _grids.Clear();
                _mapManager.FindGridsIntersecting(mapId, worldBounds, ref _grids);

                foreach (var grid in _grids)
                {
                    var invGridMatrix = _xform.GetInvWorldMatrix(grid.Owner);
                    var localBounds = invGridMatrix.TransformBox(worldBounds);
                    _tileBounds.Clear();
                    _map.GetLocalTileBounds(grid.Owner, grid, localBounds, _tileBounds, predicate);

                    var worldToTextureMatrix = Matrix3x2.Multiply(_xform.GetWorldMatrix(grid.Owner), invMatrix);
                    worldHandle.SetTransform(worldToTextureMatrix);
                    _rects.Clear();

                    foreach (var bounds in _tileBounds)
                    {
                        _rects.Add(new WorldRect(bounds, Color.White));
                    }

                    worldHandle.DrawRectsUnmodulated(CollectionsMarshal.AsSpan(_rects));
                }
            },
            Color.Transparent);

        return targetRes.Target!;
    }

    /// <summary>
    /// Returns a viewport-sized texture where tiles accepted by <paramref name="predicate"/> are white and everything else is transparent.
    /// </summary>
    public IRenderTexture GetTileStencil(
        in OverlayDrawArgs args,
        string key,
        string name,
        TileStencilPredicate predicate)
    {
        var viewport = args.Viewport;
        var target = viewport.RenderTarget;
        var res = _resources.GetForViewport(viewport, static _ => new CachedResources());
        var targetRes = res.GetOrCreate(key);

        if (targetRes.Target?.Texture.Size != target.Size)
        {
            targetRes.Target?.Dispose();
            targetRes.Target = _clyde.CreateRenderTarget(target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: name);
            targetRes.LastFrame = 0;
        }

        var worldHandle = args.WorldHandle;
        var invMatrix = viewport.GetWorldToLocalMatrix();
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;

        if (targetRes.LastFrame == _timing.CurFrame &&
            targetRes.LastMapId == mapId &&
            targetRes.LastWorldBounds.Equals(worldBounds))
        {
            return targetRes.Target!;
        }

        targetRes.LastFrame = _timing.CurFrame;
        targetRes.LastMapId = mapId;
        targetRes.LastWorldBounds = worldBounds;

        worldHandle.RenderInRenderTarget(targetRes.Target,
            () =>
            {
                _grids.Clear();
                _mapManager.FindGridsIntersecting(mapId, worldBounds, ref _grids);

                foreach (var grid in _grids)
                {
                    var transform = _xform.GetWorldMatrix(grid.Owner);
                    var worldToTextureMatrix = Matrix3x2.Multiply(transform, invMatrix);
                    var tiles = _map.GetTilesEnumerator(grid.Owner, grid, worldBounds);
                    worldHandle.SetTransform(worldToTextureMatrix);
                    _rects.Clear();

                    while (tiles.MoveNext(out var tileRef))
                    {
                        if (!predicate(grid, tileRef))
                            continue;

                        var bounds = _lookup.GetLocalBounds(tileRef, grid.Comp.TileSize);
                        _rects.Add(new WorldRect(bounds, Color.White));
                    }

                    worldHandle.DrawRectsUnmodulated(CollectionsMarshal.AsSpan(_rects));
                }
            },
            Color.Transparent);

        return targetRes.Target!;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _resources.Dispose();
    }

    private sealed class CachedResources : IDisposable
    {
        private readonly Dictionary<string, CachedTarget> _targets = new();

        public CachedTarget GetOrCreate(string key)
        {
            if (_targets.TryGetValue(key, out var target))
                return target;

            target = new CachedTarget();
            _targets.Add(key, target);
            return target;
        }

        public void Dispose()
        {
            foreach (var target in _targets.Values)
            {
                target.Target?.Dispose();
            }

            _targets.Clear();
        }
    }

    private sealed class CachedTarget
    {
        public IRenderTexture? Target;
        public uint LastFrame;
        public MapId LastMapId;
        public Box2Rotated LastWorldBounds;
    }
}

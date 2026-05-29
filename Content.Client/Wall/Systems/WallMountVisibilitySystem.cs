using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Wall.Systems;

/// <summary>
/// Manages the directional visibility overlay for wall-mounted entities.
/// </summary>
public sealed partial class WallMountVisibilitySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private WallMountTreeSystem _tree = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    /// <summary>
    /// Tags that block visibility when present on the same tile.
    /// </summary>
    private static readonly ProtoId<TagPrototype>[] BlockingTags = ["Wall"];

    /// <summary>
    /// Caches for whether a tile has any blocking entity.
    /// </summary>
    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), bool> _tileCache = [];

    private WallMountVisibilityOverlay _overlayInstance = default!;

    /// <summary>
    /// Whether directional visibility is currently enabled.
    /// </summary>
    internal bool DirectionalVisibilityEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagComponent, AnchorStateChangedEvent>(OnTagAnchorChanged);

        SubscribeLocalEvent<WallMountComponent, ComponentShutdown>(OnWallMountShutdown);
        SubscribeLocalEvent<WallMountComponent, AfterAutoHandleStateEvent>(OnWallMountAfterHandleState);

        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        _overlayInstance = new WallMountVisibilityOverlay(_timing, _map, _sprite, _xform, _tree, this, _gridQuery, _spriteQuery);

        Subs.CVar(_cfg, CCVars.WallMountDirectionalVisibility, OnDirectionalVisibilityChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_overlayInstance);
    }

    private void OnDirectionalVisibilityChanged(bool enabled)
    {
        DirectionalVisibilityEnabled = enabled;

        if (enabled)
        {
            _overlay.AddOverlay(_overlayInstance);
        }
        else
        {
            _overlay.RemoveOverlay(_overlayInstance);
            SetAllVisible(true);
        }
    }

    /// <summary>
    /// Invalidates tile cache when anchor state changes for a blocking entity.
    /// </summary>
    private void OnTagAnchorChanged(Entity<TagComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!_tag.HasAnyTag(ent.Comp, BlockingTags))
            return;

        var xform = args.Transform;
        if (xform.GridUid is not { } gridUid)
            return;

        if (!_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        _tileCache.Remove((gridUid, tile));
    }

    /// <summary>
    /// Makes the entity visible again on component shutdown.
    /// </summary>
    private void OnWallMountShutdown(Entity<WallMountComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Makes the entity visible again if directional visibility is disabled for this mount.
    /// </summary>
    private void OnWallMountAfterHandleState(Entity<WallMountComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.DirectionalVisibility)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Removes all cached entries for a grid that is being removed.
    /// </summary>
    private void OnGridRemoval(GridRemovalEvent ev)
    {
        foreach (var key in _tileCache.Keys.Where(k => k.Grid == ev.EntityUid).ToList())
            _tileCache.Remove(key);
    }

    /// <summary>
    /// Clears tile cache and resets all wall-mount visibility on round restart.
    /// </summary>
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _tileCache.Clear();
        SetAllVisible(true);
    }

    /// <summary>
    /// Forces all wall-mount entities to become visible or hidden.
    /// </summary>
    internal void SetAllVisible(bool visible)
    {
        var query = AllEntityQuery<WallMountComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _sprite.SetVisible((uid, sprite), visible);
        }
    }

    /// <summary>
    /// Checks whether the tile contains any anchored blocking entity.
    /// </summary>
    internal bool IsTileBlocked(EntityUid gridUid, Vector2i tile, EntityUid? ignoreUid = null)
    {
        if (!_gridQuery.TryGetComponent(gridUid, out var grid))
            return false;

        var key = (gridUid, tile);
        if (_tileCache.TryGetValue(key, out var cached))
            return cached;

        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (enumerator.MoveNext(out var anchored))
        {
            if (anchored == ignoreUid)
                continue;

            if (!_tag.HasAnyTag(anchored.Value, BlockingTags))
                continue;

            return _tileCache[key] = true;
        }

        return _tileCache[key] = false;
    }
}

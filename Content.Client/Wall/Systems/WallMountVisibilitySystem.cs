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

namespace Content.Client.Wall.Systems;

/// <summary>
/// Manages the directional visibility overlay for wall-mounted entities.
/// </summary>
public sealed class WallMountVisibilitySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly WallMountTreeSystem _tree = default!;
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private readonly EntityQuery<MapGridComponent> _gridQuery = default!;

    // Tags that block visibility when present on the same tile
    private static readonly ProtoId<TagPrototype>[] BlockingTags = ["Wall"];

    // Cache for whether a tile has any blocking entity
    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), bool> _tileCache = [];

    private WallMountVisibilityOverlay _overlayInstance = default!;

    internal bool DirectionalVisibilityEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagComponent, AnchorStateChangedEvent>(OnTagAnchorChanged);

        SubscribeLocalEvent<WallMountComponent, ComponentShutdown>(OnWallMountShutdown);
        SubscribeLocalEvent<WallMountComponent, AfterAutoHandleStateEvent>(OnWallMountAfterHandleState);

        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => _tileCache.Clear());

        _overlayInstance = new WallMountVisibilityOverlay(_xform, _sprite, _tree, _map, this, _spriteQuery, _gridQuery);

        Subs.CVar(_cfg, CCVars.WallMountDirectionalVisibility, OnDirectionalVisibilityChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay(_overlayInstance);
    }

    private void OnDirectionalVisibilityChanged(bool value)
    {
        DirectionalVisibilityEnabled = value;
        if (value)
        {
            _overlay.AddOverlay(_overlayInstance);
        }
        else
        {
            _overlay.RemoveOverlay(_overlayInstance);
            SetAllVisible(true);
        }
    }

    // Invalidate tile cache when anchor state changes for a blocking entity
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

    // Make visible again on component shutdown
    private void OnWallMountShutdown(Entity<WallMountComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetVisible((ent, sprite), true);
    }

    // Make visible again if directional visibility is disabled for this mount
    private void OnWallMountAfterHandleState(Entity<WallMountComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!ent.Comp.DirectionalVisibility && TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetVisible((ent, sprite), true);
    }

    // Remove all cached entries for a grid that is being removed
    private void OnGridRemoval(GridRemovalEvent ev)
    {
        var keysToRemove = _tileCache.Keys.Where(k => k.Grid == ev.EntityUid).ToList();
        foreach (var key in keysToRemove)
            _tileCache.Remove(key);
    }

    // Force all mounts to become visible
    private void SetAllVisible(bool visible)
    {
        var query = AllEntityQuery<WallMountComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _sprite.SetVisible((uid, sprite), visible);
        }
    }

    // Check whether the tile contains any anchored blocking entity
    internal bool IsTileBlocked(EntityUid gridUid, Vector2i tile, EntityUid? ignoreUid = null)
    {
        if (!_gridQuery.TryGetComponent(gridUid, out var grid))
            return false;

        var key = (gridUid, tile);

        if (_tileCache.TryGetValue(key, out var cached))
            return cached;

        var isBlocked = false;
        var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchoredEnumerator.MoveNext(out var anchored))
        {
            if (anchored == ignoreUid)
                continue;

            if (_tag.HasAnyTag(anchored.Value, BlockingTags))
            {
                isBlocked = true;
                break;
            }
        }

        _tileCache[key] = isBlocked;
        return isBlocked;
    }
}

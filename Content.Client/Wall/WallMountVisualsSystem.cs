using System.Linq;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Wall;

/// <summary>
/// Hides wall-mounted entities when the local player is outside their facing arc.
/// </summary>
public sealed class WallMountVisualsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    // Tags that block visibility when present on the same tile
    private static readonly ProtoId<TagPrototype>[] BlockingTags = ["Wall"];

    // Cache for whether a tile has any blocking entity
    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), bool> _tileCache = [];

    private EntityQuery<MapGridComponent> _gridQuery;

    private bool _prevFovEnabled = true;
    private bool _directionalVisibilityEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<WallMountComponent, ComponentShutdown>(OnWallMountShutdown);
        SubscribeLocalEvent<WallMountComponent, AfterAutoHandleStateEvent>(OnWallMountAfterHandleState);

        SubscribeLocalEvent<TagComponent, AnchorStateChangedEvent>(OnTagAnchorChanged);

        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => _tileCache.Clear());

        Subs.CVar(_cfg, CCVars.WallMountDirectionalVisibility, value =>
        {
            _directionalVisibilityEnabled = value;

            if (!_directionalVisibilityEnabled)
                SetAllVisible();
        }, true);
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

    // Invalidate tile cache when anchor state changes for a blocking entity
    private void OnTagAnchorChanged(Entity<TagComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!_tag.HasAnyTag(ent.Comp, BlockingTags))
            return;

        var xform = args.Transform;
        if (xform.GridUid is not { } gridUid)
            return;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        _tileCache.Remove((gridUid, tile));
    }

    // Remove all cached entries for a grid that is being removed
    private void OnGridRemoval(GridRemovalEvent ev)
    {
        var keysToRemove = _tileCache.Keys.Where(k => k.Grid == ev.EntityUid).ToList();
        foreach (var key in keysToRemove)
            _tileCache.Remove(key);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_directionalVisibilityEnabled)
            return;

        var fovEnabled = _eye.CurrentEye?.DrawFov ?? true;

        // Show all mounts when FOV is disabled
        if (!fovEnabled)
        {
            if (_prevFovEnabled)
            {
                _prevFovEnabled = false;
                SetAllVisible();
            }
            return;
        }

        _prevFovEnabled = true;

        var player = _player.LocalEntity;
        if (player is null)
            return;

        var playerMapPos = _transform.GetMapCoordinates(player.Value);
        var viewBounds = _eye.GetWorldViewbounds();

        var query = AllEntityQuery<WallMountComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mount, out var sprite, out var xform))
        {
            if (!mount.DirectionalVisibility)
                continue;

            // Different map = visible
            if (playerMapPos.MapId != xform.MapID)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            // Full 360 degree arc = visible
            if (mount.Arc >= Math.Tau)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            // Not on a grid = visible
            if (xform.GridUid is null)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            var (mountWorldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
            // Outside viewport = visible
            if (!viewBounds.Contains(mountWorldPos))
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            // No blocking entity on the same tile = visible
            if (!IsBlockedTile(uid, xform))
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            var visible = IsWithinFacingArc(mount, playerMapPos, mountWorldPos, worldRot);
            _sprite.SetVisible((uid, sprite), visible);
        }
    }

    // Check whether the tile contains any anchored blocking entity
    private bool IsBlockedTile(EntityUid uid, TransformComponent xform)
    {
        var gridUid = xform.GridUid!.Value;
        if (!_gridQuery.TryGetComponent(gridUid, out var grid))
            return false;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var key = (gridUid, tile);

        if (_tileCache.TryGetValue(key, out var cached))
            return cached;

        var isBlocked = false;

        var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchoredEnumerator.MoveNext(out var anchored))
        {
            if (anchored == uid)
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

    // Force all mounts to become visible
    private void SetAllVisible()
    {
        var query = AllEntityQuery<WallMountComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _sprite.SetVisible((uid, sprite), true);
        }
    }

    // Whether the player's position is within the mount's facing arc
    private static bool IsWithinFacingArc(WallMountComponent mount, MapCoordinates playerPos, Vector2 mountWorldPos, Angle worldRot)
    {
        var toPlayer = playerPos.Position - mountWorldPos;
        var facingAngle = worldRot + mount.Direction;
        var angleToPlayer = Angle.FromWorldVec(toPlayer);
        var angleDelta = (facingAngle - angleToPlayer).Reduced().FlipPositive();
        var halfArc = mount.Arc / 2;

        return angleDelta < halfArc || Math.Tau - angleDelta < halfArc;
    }
}

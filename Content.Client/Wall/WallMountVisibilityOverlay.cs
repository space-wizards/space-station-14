using System.Numerics;
using Content.Client.Wall.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.Wall;

/// <summary>
/// Renders wall-mounted entities conditionally based on their facing arc relative to the viewport's eye.
/// </summary>
public sealed class WallMountVisibilityOverlay(
    TransformSystem xform,
    SpriteSystem sprite,
    WallMountTreeSystem tree,
    SharedMapSystem map,
    WallMountVisibilitySystem visibility,
    EntityQuery<SpriteComponent> spriteQuery,
    EntityQuery<MapGridComponent> gridQuery) : Overlay
{
    private readonly TransformSystem _xform = xform;
    private readonly SpriteSystem _sprite = sprite;
    private readonly WallMountTreeSystem _tree = tree;
    private readonly SharedMapSystem _map = map;
    private readonly WallMountVisibilitySystem _visibility = visibility;
    private readonly EntityQuery<SpriteComponent> _spriteQuery = spriteQuery;
    private readonly EntityQuery<MapGridComponent> _gridQuery = gridQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_visibility.DirectionalVisibilityEnabled)
            return;

        if (args.Viewport.Eye is null)
            return;

        var eye = args.Viewport.Eye;

        if (!eye.DrawFov)
        {
            foreach (var entity in _tree.QueryAabb(args.MapId, args.WorldBounds))
            {
                if (_spriteQuery.TryGetComponent(entity.Uid, out var sprite))
                    _sprite.SetVisible((entity.Uid, sprite), true);
            }
            return;
        }

        var matrix = args.Viewport.GetWorldToLocalMatrix();
        var entities = _tree.QueryAabb(args.MapId, args.WorldBounds);

        foreach (var entity in entities)
        {
            var (wallmount, xform) = entity;
            var uid = entity.Uid;

            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            if (!wallmount.DirectionalVisibility)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            if (wallmount.Arc >= Math.Tau)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            if (xform.GridUid is not { } gridUid)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            if (!_gridQuery.TryGetComponent(gridUid, out var grid))
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
            var isTileBlocked = _visibility.IsTileBlocked(gridUid, tile, uid);

            var (pos, rot) = _xform.GetWorldPositionRotation(xform);
            var wallmountScreenRotation = rot + eye.Rotation + wallmount.Direction;

            var entityScreenPos = Vector2.Transform(pos, matrix);
            var eyeScreenPos = Vector2.Transform(eye.Position.Position, matrix);
            var dist = entityScreenPos - eyeScreenPos;
            var distAngle = (dist with { X = -dist.X }).ToWorldAngle();
            var angleBetween = Angle.ShortestDistance(distAngle, wallmountScreenRotation);
            var halfArc = wallmount.Arc / 2;
            var withinArc = angleBetween > -halfArc && angleBetween < halfArc;

            var visible = !isTileBlocked || withinArc;
            _sprite.SetVisible((uid, sprite), visible);
        }
    }
}

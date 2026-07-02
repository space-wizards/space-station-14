using System.Linq;
using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Wall.Systems;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Wall;

/// <summary>
/// Renders wall-mounted entities conditionally based on their facing arc relative to the viewport's eye.
/// </summary>
public sealed partial class WallMountVisibilityOverlay(
    IGameTiming timing,
    SharedMapSystem map,
    SpriteSystem sprite,
    TransformSystem xform,
    WallMountTreeSystem tree,
    WallMountVisibilitySystem visibility,
    EntityQuery<MapGridComponent> gridQuery,
    EntityQuery<SpriteComponent> spriteQuery) : Overlay
{
    private readonly IGameTiming _timing = timing;
    private readonly SharedMapSystem _map = map;
    private readonly SpriteSystem _sprite = sprite;
    private readonly TransformSystem _xform = xform;
    private readonly WallMountTreeSystem _tree = tree;
    private readonly WallMountVisibilitySystem _visibility = visibility;

    private readonly EntityQuery<MapGridComponent> _gridQuery = gridQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery = spriteQuery;

    /// <summary>
    /// Caches <see cref="ViewportFadeState"/> instances per viewport.
    /// </summary>
    private readonly OverlayResourceCache<ViewportFadeState> _fadeCache = new();

    /// <summary>
    /// Original sprite alphas, shared across viewports to avoid capturing values modified by another viewport.
    /// </summary>
    private readonly Dictionary<EntityUid, float> _originalAlphas = [];

    /// <summary>
    /// Alpha change per second during fade.
    /// </summary>
    private const float FadeSpeed = 9f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_visibility.DirectionalVisibilityEnabled)
            return;

        if (args.Viewport.Eye is not { } eye)
            return;

        var viewportState = _fadeCache.GetForViewport(args.Viewport, _ => new ViewportFadeState(_sprite, _spriteQuery));

        if (!eye.DrawFov)
        {
            HandleFovDisabled(args, viewportState);
            return;
        }

        viewportState.WasFovEnabled = true;

        var fadeStep = FadeSpeed * (float)_timing.FrameTime.TotalSeconds;
        var matrix = args.Viewport.GetWorldToLocalMatrix();

        viewportState.SeenThisFrame.Clear();
        ProcessVisibleEntities(args, eye, matrix, fadeStep, viewportState);

        // Remove entities that left the viewport this frame.
        foreach (var uid in viewportState.FadeStates.Keys.Except(viewportState.SeenThisFrame).ToList())
            RemoveTrackedEntity(uid, viewportState);

        ApplyFadeToVisibleEntities(viewportState);
    }

    /// <summary>
    /// When FOV gets disabled, clears fade state and restores sprites.
    /// On subsequent frames, keeps wall-mounts visible and restores alpha modified by other viewports.
    /// </summary>
    private void HandleFovDisabled(in OverlayDrawArgs args, ViewportFadeState viewportState)
    {
        if (viewportState.WasFovEnabled)
        {
            ClearViewportFadeState(viewportState);
            viewportState.WasFovEnabled = false;
        }

        foreach (var entity in _tree.QueryAabb(args.MapId, args.WorldBounds))
        {
            var uid = entity.Uid;
            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            if (_originalAlphas.Remove(uid, out var origAlpha))
                _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(origAlpha));

            _sprite.SetVisible((uid, sprite), true);
        }
    }

    /// <summary>
    /// Updates fade state for all wall-mounted entities in the viewport.
    /// </summary>
    private void ProcessVisibleEntities(in OverlayDrawArgs args, IEye eye, Matrix3x2 matrix, float fadeStep, ViewportFadeState viewportState)
    {
        foreach (var entity in _tree.QueryAabb(args.MapId, args.WorldBounds))
        {
            var (wallmount, xform) = entity;
            var uid = entity.Uid;

            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            // Capture original alpha before any viewport modifies it.
            if (!_originalAlphas.TryGetValue(uid, out var originalAlpha))
                _originalAlphas[uid] = originalAlpha = sprite.Color.A;

            viewportState.SeenThisFrame.Add(uid);

            var targetAlpha = ComputeTargetAlpha(uid, wallmount, xform, eye, matrix);
            UpdateFadeState(uid, originalAlpha, targetAlpha, fadeStep, viewportState);
        }
    }

    /// <summary>
    /// Returns 1 if the entity is within its facing arc relative to the eye, 0 otherwise.
    /// </summary>
    private float ComputeTargetAlpha(EntityUid uid, WallMountComponent wallmount, TransformComponent xform, IEye eye, Matrix3x2 matrix)
    {
        if (!wallmount.DirectionalVisibility || wallmount.Arc >= Math.Tau)
            return 1f;

        if (xform.GridUid is not { } gridUid || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return 1f;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        if (!_visibility.IsTileBlocked(gridUid, tile, uid))
            return 1f;

        var (pos, rot) = _xform.GetWorldPositionRotation(xform);
        var facingAngle = rot + eye.Rotation + wallmount.Direction;

        var entityScreenPos = Vector2.Transform(pos, matrix);
        var eyeScreenPos = Vector2.Transform(eye.Position.Position, matrix);
        var toEntity = entityScreenPos - eyeScreenPos;
        var eyeToEntityAngle = (toEntity with { X = -toEntity.X }).ToWorldAngle();

        var angleDiff = Angle.ShortestDistance(eyeToEntityAngle, facingAngle);
        return Math.Abs(angleDiff) < wallmount.Arc / 2 ? 1f : 0f;
    }

    /// <summary>
    /// Applies the current fade state to all entities seen this frame.
    /// </summary>
    private void ApplyFadeToVisibleEntities(ViewportFadeState viewportState)
    {
        foreach (var uid in viewportState.SeenThisFrame)
        {
            if (!viewportState.FadeStates.TryGetValue(uid, out var state))
                continue;

            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
            {
                RemoveTrackedEntity(uid, viewportState);
                continue;
            }

            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(state.EffectiveAlpha));
            _sprite.SetVisible((uid, sprite), state.IsVisible);
        }
    }

    /// <summary>
    /// Updates fade target for a tracked entity. Newly seen entities snap immediately without fading.
    /// </summary>
    private static void UpdateFadeState(EntityUid uid, float originalAlpha, float targetAlpha, float fadeStep, ViewportFadeState viewportState)
    {
        if (viewportState.FadeStates.TryGetValue(uid, out var state))
        {
            state.TargetAlpha = targetAlpha;
            state.StepTowards(fadeStep);
            viewportState.FadeStates[uid] = state;
            return;
        }

        viewportState.FadeStates[uid] = WallMountFadeState.Snapped(originalAlpha, targetAlpha);
    }

    private void RestoreSprite(EntityUid uid, WallMountFadeState state)
    {
        if (!_spriteQuery.TryGetComponent(uid, out var sprite))
            return;

        _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(state.OriginalAlpha));
        _sprite.SetVisible((uid, sprite), true);
    }

    /// <summary>
    /// Removes an entity from fade tracking and restores its sprite.
    /// </summary>
    private void RemoveTrackedEntity(EntityUid uid, ViewportFadeState viewportState)
    {
        if (viewportState.FadeStates.Remove(uid, out var state))
            RestoreSprite(uid, state);

        _originalAlphas.Remove(uid);
    }

    /// <summary>
    /// Clears all fade tracking for a viewport and restores sprites.
    /// </summary>
    private void ClearViewportFadeState(ViewportFadeState viewportState)
    {
        foreach (var (uid, state) in viewportState.FadeStates)
        {
            RestoreSprite(uid, state);
            _originalAlphas.Remove(uid);
        }

        viewportState.FadeStates.Clear();
        viewportState.SeenThisFrame.Clear();
    }

    protected override void DisposeBehavior()
    {
        _fadeCache.Dispose();
        base.DisposeBehavior();
    }
}

using System.Numerics;
using Content.Shared.Disposal.Traversal;
using Content.Shared.Disposal.Unit;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.Disposal.Traversal;

/// <summary>
/// Draws the reachable traversal pipe network for the local player.
/// </summary>
public sealed partial class DisposalTraversalPipeOverlay : Overlay
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform;
    private readonly DisposalTraversalVisionSystem _vision;

    private const float GlowRadius = 0.015f;
    private static readonly Color CurrentPipeGlowColor = new(1.0f, 0.0f, 0.0f, 0.65f);
    private static readonly Color PipeBaseColor = new(1.0f, 1.0f, 1.0f, 0.45f);
    private static readonly Vector2[] GlowOffsets =
    {
        new(-GlowRadius, 0),
        new(GlowRadius, 0),
        new(0, -GlowRadius),
        new(0, GlowRadius),
    };

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public DisposalTraversalPipeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entityManager.System<SpriteSystem>();
        _lookup = _entityManager.System<EntityLookupSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();
        _vision = _entityManager.System<DisposalTraversalVisionSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        if (!_entityManager.TryGetComponent<BeingDisposedComponent>(player, out var beingDisposed))
            return;

        if (!_entityManager.TryGetComponent<DisposalTraversalHolderComponent>(beingDisposed.Holder, out var holder)
            || holder.CurrentTube == null)
            return;

        if (!_entityManager.TryGetComponent<EyeComponent>(player.Value, out var eye))
            return;

        var reachableTubes = _vision.ReachableTubes;
        if (reachableTubes == null)
            return;

        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var eyeRot = eye.Rotation;

        var entities = _lookup.GetEntitiesIntersecting(
            args.MapId,
            bounds,
            LookupFlags.Uncontained);

        worldHandle.UseShader(null);

        foreach (var uid in entities)
        {
            if (!reachableTubes.Contains(uid))
                continue;

            if (!_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            if (!sprite.Visible)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);
            var worldRot = _transform.GetWorldRotation(uid);

            var oldColor = sprite.Color;

            if (holder.CurrentTube == uid)
            {
                _spriteSystem.SetColor((uid, sprite), CurrentPipeGlowColor);

                foreach (var offset in GlowOffsets)
                {
                    _spriteSystem.RenderSprite(
                        (uid, sprite),
                        worldHandle,
                        eyeRot,
                        worldRot,
                        worldPos + offset);
                }
            }

            _spriteSystem.SetColor((uid, sprite), PipeBaseColor);

            _spriteSystem.RenderSprite(
                (uid, sprite),
                worldHandle,
                eyeRot,
                worldRot,
                worldPos);

            _spriteSystem.SetColor((uid, sprite), oldColor);
        }
    }
}

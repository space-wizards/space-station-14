using Content.Client.Hands;
using Content.Shared.SubFloor;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.SubFloor;

public sealed class TrayScannerSystem : SharedTrayScannerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string TRayAnimationKey = "trays";
    private const double AnimationLength = 0.5;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO: Multiple viewports or w/e
        var player = _player.LocalPlayer?.ControlledEntity;
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(player, out var playerXform))
            return;

        var playerPos = _transform.GetWorldPosition(playerXform, xformQuery);
        var playerMap = playerXform.MapID;
        var range = 0f;

        if (TryComp<HandsComponent>(player, out var playerHands) &&
            TryComp<TrayScannerComponent>(playerHands.ActiveHandEntity, out var scanner) && scanner.Enabled)
        {
            range = scanner.Range;

            foreach (var comp in _lookup.GetComponentsInRange<SubFloorHideComponent>(playerMap, playerPos, range))
            {
                var uid = comp.Owner;
                if (!comp.IsUnderCover || !comp.BlockAmbience | !comp.BlockInteractions)
                    continue;

                EnsureComp<TrayRevealedComponent>(uid);
            }
        }

        var revealedQuery = AllEntityQuery<TrayRevealedComponent, SpriteComponent, TransformComponent>();
        var subfloorQuery = GetEntityQuery<SubFloorHideComponent>();

        while (revealedQuery.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            // Revealing
            // Add buffer range to avoid flickers.
            if (subfloorQuery.HasComponent(uid) &&
                xform.MapID != MapId.Nullspace &&
                xform.MapID == playerMap &&
                xform.Anchored &&
                range != 0f &&
                (playerPos - worldPos).Length <= range + 0.5f)
            {
                // Due to the fact client is predicting this server states will reset it constantly
                if ((!_appearance.TryGetData(uid, SubFloorVisuals.ScannerRevealed, out bool value) || !value) &&
                    sprite.Color.A > SubfloorRevealAlpha)
                {
                    sprite.Color = sprite.Color.WithAlpha(0f);
                }

                SetRevealed(uid, true);

                if (sprite.Color.A >= SubfloorRevealAlpha || _animation.HasRunningAnimation(uid, TRayAnimationKey))
                    continue;

                _animation.Play(uid, new Animation()
                {
                    Length = TimeSpan.FromSeconds(AnimationLength),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty()
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Color),
                            KeyFrames =
                            {
                                new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), 0f),
                                new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(SubfloorRevealAlpha), (float) AnimationLength)
                            }
                        }
                    }
                }, TRayAnimationKey);
            }
            // Hiding
            else
            {
                // Hidden completely so unreveal and reset the alpha.
                if (sprite.Color.A <= 0f)
                {
                    SetRevealed(uid, false);
                    RemCompDeferred<TrayRevealedComponent>(uid);
                    sprite.Color = sprite.Color.WithAlpha(1f);
                    continue;
                }

                SetRevealed(uid, true);

                if (_animation.HasRunningAnimation(uid, TRayAnimationKey))
                    continue;

                _animation.Play(uid, new Animation()
                {
                    Length = TimeSpan.FromSeconds(AnimationLength),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty()
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Color),
                            KeyFrames =
                            {
                                new AnimationTrackProperty.KeyFrame(sprite.Color, 0f),
                                new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), (float) AnimationLength)
                            }
                        }
                    }
                }, TRayAnimationKey);
            }
        }
    }

    private void SetRevealed(EntityUid uid, bool value)
    {
        _appearance.SetData(uid, SubFloorVisuals.ScannerRevealed, value);
    }
}

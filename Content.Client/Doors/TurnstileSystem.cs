using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Doors;

/// <inheritdoc/>
public sealed class TurnstileSystem : SharedTurnstileSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    private const string AnimationKey = "Turnstile";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnstileComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAnimationCompleted(Entity<TurnstileComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        sprite.LayerSetState(TurnstileVisualLayers.Base, new RSI.StateId(ent.Comp.DefaultState));
    }

    protected override void PlayAnimation(EntityUid uid, string stateId)
    {
        if (!TryComp<AnimationPlayerComponent>(uid, out var animation) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;
        var ent = (uid, animation);

        if (_animationPlayer.HasRunningAnimation(animation, AnimationKey))
            _animationPlayer.Stop(ent, AnimationKey);

        if (sprite.BaseRSI == null || !sprite.BaseRSI.TryGetState(stateId, out var state))
            return;
        var animLength = state.AnimationLength;

        var anim = new Animation
        {
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = TurnstileVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(state.StateId, 0f),
                    },
                },
            },
            Length = TimeSpan.FromSeconds(animLength),
        };

        _animationPlayer.Play(ent, anim, AnimationKey);
    }
}

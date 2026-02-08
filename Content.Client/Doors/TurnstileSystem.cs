using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Doors;

/// <inheritdoc/>
public sealed class TurnstileSystem : SharedTurnstileSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly EntProtoId ExamineArrow = "TurnstileArrow";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnstileComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<TurnstileComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAnimationCompleted(Entity<TurnstileComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key == nameof(TurnstileVisualLayers.Indicators))
        {
            _sprite.LayerSetVisible(ent.Owner, TurnstileVisualLayers.Indicators, false);
        }
        else if (args.Key != nameof(TurnstileVisualLayers.Spinner))
        {
            _sprite.LayerSetRsiState(ent.Owner, TurnstileVisualLayers.Spinner, new RSI.StateId(ent.Comp.DefaultState));
        }
    }

    private void OnExamined(Entity<TurnstileComponent> ent, ref ExaminedEvent args)
    {
        Spawn(ExamineArrow, new EntityCoordinates(ent, 0, 0));
    }

    protected override void PlayAnimation(EntityUid uid, TurnstileVisualLayers layer, string stateId)
    {
        if (!TryComp<AnimationPlayerComponent>(uid, out var animation) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;
        var ent = (uid, animation);

        if (_animationPlayer.HasRunningAnimation(animation, layer.ToString()))
            _animationPlayer.Stop(ent, layer.ToString());

        if (sprite.BaseRSI == null || !sprite.BaseRSI.TryGetState(stateId, out var state))
            return;
        var animLength = state.AnimationLength;

        var anim = new Animation
        {
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = layer,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(state.StateId, 0f),
                    },
                },
            },
            Length = TimeSpan.FromSeconds(animLength),
        };

        _sprite.LayerSetVisible(uid, layer, true);
        _animationPlayer.Play(ent, anim, layer.ToString());
    }
}

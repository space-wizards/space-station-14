using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
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
        SubscribeLocalEvent<TurnstileComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<TurnstileComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnComponentStartup(Entity<TurnstileComponent> ent, ref ComponentStartup args)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, null);

        if (!Resolve(spriteEnt, ref spriteEnt.Comp))
            return;

        if (!_sprite.TryGetLayer(spriteEnt, TurnstileVisualLayers.Spinner, out var layer, true))
            return;

        SetSpinnerAutoAnimation(ent, false);
    }

    private void OnAppearanceChanged(Entity<TurnstileComponent> ent, ref AppearanceChangeEvent args)
    {
        args.AppearanceData.TryGetValue(TurnstileVisuals.AccessBrokenSpinning, out var brokenSpinningObj);

        if (brokenSpinningObj is not bool brokenSpinning)
            return;

        SetSpinnerAutoAnimation(ent, brokenSpinning);
    }

    private void OnAnimationCompleted(Entity<TurnstileComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key == $"turnstile-anim-{nameof(TurnstileVisualLayers.Indicators)}")
        {
            _sprite.LayerSetVisible(ent.Owner, TurnstileVisualLayers.Indicators, false);
        }
        else if (args.Key != $"turnstile-anim-{nameof(TurnstileVisualLayers.Spinner)}")
        {
            SetSpinnerAutoAnimation(ent, false);
        }
    }

    private void StopAnimation(EntityUid uid,
        TurnstileVisualLayers layer,
        AnimationPlayerComponent? player = null)
    {
        if (!Resolve(uid, ref player, logMissing: false))
            return;
        var ent = (uid, player);

        // Use the name of the layer for the animation key
        var animKey = $"turnstile-anim-{layer.ToString()}";
        if (_animationPlayer.HasRunningAnimation(player, animKey))
            _animationPlayer.Stop(ent, animKey);
    }

    private void SetSpinnerAutoAnimation(Entity<TurnstileComponent> ent, bool value)
    {
        if (value) // Cancel currently running animation
            StopAnimation(ent, TurnstileVisualLayers.Spinner);
        else // Reset back to frame 0
            _sprite.LayerSetAnimationTime(ent.Owner, TurnstileVisualLayers.Spinner, 0f);

        // Set or unset constant repeating animation mode
        _sprite.LayerSetAutoAnimated(ent.Owner, TurnstileVisualLayers.Spinner, value);
    }

    private void OnExamined(Entity<TurnstileComponent> ent, ref ExaminedEvent args)
    {
        Spawn(ExamineArrow, new EntityCoordinates(ent, 0, 0));
    }

    protected override void PlayAnimation(EntityUid uid, TurnstileVisualLayers layer, string stateId)
    {
        if (!TryComp<AnimationPlayerComponent>(uid, out var animation) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        StopAnimation(uid, layer, animation);

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
        _animationPlayer.Play(uid, anim, $"turnstile-anim-{layer.ToString()}");
    }
}

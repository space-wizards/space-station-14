using Content.Shared.Light;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Client.Light.Visualizers;

public sealed class PoweredLightVisualizerSystem : VisualizerSystem<PoweredLightVisualsComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    protected override void OnAppearanceChange(EntityUid uid, PoweredLightVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<PoweredLightState>(uid, PoweredLightVisuals.BulbState, out var state, args.Component))
            return;

        if (comp.SpriteStateMap.TryGetValue(state, out var spriteState))
            args.Sprite.LayerSetState(PoweredLightLayers.Base, spriteState);

        if (args.Sprite.LayerExists(PoweredLightLayers.Glow))
        {
            if (TryComp<PointLightComponent>(uid, out var light))
            {
                args.Sprite.LayerSetColor(PoweredLightLayers.Glow, light.Color);
            }

            args.Sprite.LayerSetVisible(PoweredLightLayers.Glow, state == PoweredLightState.On);
        }

        SetBlinkingAnimation(
            uid,
            state == PoweredLightState.On
            && (AppearanceSystem.TryGetData<bool>(uid, PoweredLightVisuals.Blinking, out var isBlinking, args.Component) && isBlinking),
            comp
        );
    }

    /// <summary>
    /// Loops the blinking animation until the light should stop blinking.
    /// </summary>
    private void OnAnimationCompleted(EntityUid uid, PoweredLightVisualsComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != PoweredLightVisualsComponent.BlinkingAnimationKey)
            return;

        if(!comp.IsBlinking)
            return;

        AnimationSystem.Play(uid, Comp<AnimationPlayerComponent>(uid), BlinkingAnimation(comp), PoweredLightVisualsComponent.BlinkingAnimationKey);
    }

    /// <summary>
    /// Sets whether or not the given light should be blinking.
    /// Triggers or clears the blinking animation of the state changes.
    /// </summary>
    private void SetBlinkingAnimation(EntityUid uid, bool shouldBeBlinking, PoweredLightVisualsComponent comp)
    {
        if (shouldBeBlinking == comp.IsBlinking)
            return;

        comp.IsBlinking = shouldBeBlinking;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);
        if (shouldBeBlinking)
        {
            AnimationSystem.Play(uid, animationPlayer, BlinkingAnimation(comp), PoweredLightVisualsComponent.BlinkingAnimationKey);
        }
        else if (AnimationSystem.HasRunningAnimation(uid, animationPlayer, PoweredLightVisualsComponent.BlinkingAnimationKey))
        {
            AnimationSystem.Stop(uid, animationPlayer, PoweredLightVisualsComponent.BlinkingAnimationKey);
        }
    }

    /// <summary>
    /// Generates a blinking animation.
    /// Essentially just flashes the light off and on over a random time interval.
    /// The resulting animation is looped indefinitely until the comp is set to stop blinking.
    /// </summary>
    private Animation BlinkingAnimation(PoweredLightVisualsComponent comp)
    {
        var randomTime = MathHelper.Lerp(comp.MinBlinkingAnimationCycleTime, comp.MaxBlinkingAnimationCycleTime, _random.NextFloat());
        var blinkingAnim = new Animation()
        {
            Length = TimeSpan.FromSeconds(randomTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(PointLightComponent),
                    InterpolationMode = AnimationInterpolationMode.Nearest,
                    Property = nameof(PointLightComponent.AnimatedEnable),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(false, 0),
                        new AnimationTrackProperty.KeyFrame(true, 1)
                    }
                },
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = PoweredLightLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.SpriteStateMap[PoweredLightState.Off], 0),
                        new AnimationTrackSpriteFlick.KeyFrame(comp.SpriteStateMap[PoweredLightState.On], 0.5f)
                    }
                }
            }
        };

        if (comp.BlinkingSound != null)
        {
            var sound = _audio.GetSound(comp.BlinkingSound);
            blinkingAnim.AnimationTracks.Add(new AnimationTrackPlaySound()
            {
                KeyFrames =
                {
                    new AnimationTrackPlaySound.KeyFrame(sound, 0.5f)
                }
            });
        }

        return blinkingAnim;
    }
}

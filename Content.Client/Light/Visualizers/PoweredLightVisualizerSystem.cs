using Content.Shared.Light;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Client.Light.Visualizers;

[RegisterComponent]
public sealed class PoweredLightVisualizerComponent : Component
{
    [Access(typeof(PoweredLightVisualizerSystem))]
    [DataField("minBlinkingTime")] public float MinBlinkingTime = 0.5f;
    [Access(typeof(PoweredLightVisualizerSystem))]
    [DataField("maxBlinkingTime")] public float MaxBlinkingTime = 2;
    [Access(typeof(PoweredLightVisualizerSystem))]
    [DataField("blinkingSound")] public SoundSpecifier? BlinkingSound = default;

    [Access(typeof(PoweredLightVisualizerSystem))]
    public bool WasBlinking;

    [Access(typeof(PoweredLightVisualizerSystem))]
    public Action<string>? BlinkingCallback;
}

public sealed class PoweredLightVisualizerSystem : VisualizerSystem<PoweredLightVisualizerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightVisualizerComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    protected override void OnAppearanceChange(EntityUid uid, PoweredLightVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!AppearanceSystem.TryGetData(uid, PoweredLightVisuals.BulbState, out PoweredLightState state, args.Component))
            return;

        switch (state)
        {
            case PoweredLightState.Empty:
                args.Sprite.LayerSetState(PoweredLightLayers.Base, "empty");
                ToggleBlinkingAnimation(uid, false, comp, ref args);
                break;
            case PoweredLightState.Off:
                args.Sprite.LayerSetState(PoweredLightLayers.Base, "off");
                ToggleBlinkingAnimation(uid, false, comp, ref args);
                break;
            case PoweredLightState.On:
                if (AppearanceSystem.TryGetData(uid, PoweredLightVisuals.Blinking, out bool isBlinking, args.Component))
                    ToggleBlinkingAnimation(uid, isBlinking, comp, ref args);
                if (!isBlinking)
                {
                    args.Sprite!.LayerSetState(PoweredLightLayers.Base, "on");
                }
                break;
            case PoweredLightState.Broken:
                args.Sprite.LayerSetState(PoweredLightLayers.Base, "broken");
                ToggleBlinkingAnimation(uid, false, comp, ref args);
                break;
            case PoweredLightState.Burned:
                args.Sprite.LayerSetState(PoweredLightLayers.Base, "burn");
                ToggleBlinkingAnimation(uid, false, comp, ref args);
                break;
        }
    }

    private void OnAnimationCompleted(EntityUid uid, PoweredLightVisualizerComponent comp, AnimationCompletedEvent args)
    {
        if(!comp.WasBlinking)
            return;
        if (args.Key != "blinking")
            return;
        AnimationSystem.Play(uid, Comp<AnimationPlayerComponent>(uid), BlinkingAnimation(comp), "blinking");
    }


    private void ToggleBlinkingAnimation(EntityUid uid, bool isBlinking, PoweredLightVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (isBlinking == comp.WasBlinking)
            return;
        comp.WasBlinking = isBlinking;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);
        if (isBlinking)
        {
            AnimationSystem.Play(uid, animationPlayer, BlinkingAnimation(comp), "blinking");
        }
        else if (AnimationSystem.HasRunningAnimation(uid, animationPlayer, "blinking"))
        {
            AnimationSystem.Stop(uid, animationPlayer, "blinking");
        }
    }

    private Animation BlinkingAnimation(PoweredLightVisualizerComponent comp)
    {
        var randomTime = _random.NextFloat() *
            (comp.MaxBlinkingTime - comp.MinBlinkingTime) + comp.MinBlinkingTime;

        var blinkingAnim = new Animation()
        {
            Length = TimeSpan.FromSeconds(randomTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(PointLightComponent),
                    InterpolationMode = AnimationInterpolationMode.Nearest,
                    Property = nameof(PointLightComponent.Enabled),
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
                        new AnimationTrackSpriteFlick.KeyFrame("off", 0),
                        new AnimationTrackSpriteFlick.KeyFrame("on", 0.5f)
                    }
                }
            }
        };

        if (comp.BlinkingSound != null)
        {
            blinkingAnim.AnimationTracks.Add(new AnimationTrackPlaySound()
            {
                KeyFrames =
            {
                new AnimationTrackPlaySound.KeyFrame(comp.BlinkingSound.GetSound(), 0.5f)
            }
            });
        }

        return blinkingAnim;
    }
}

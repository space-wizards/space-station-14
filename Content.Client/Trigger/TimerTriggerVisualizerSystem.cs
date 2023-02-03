using Content.Shared.Trigger;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Trigger;

public sealed class TimerTriggerVisualizerSystem : VisualizerSystem<TimerTriggerVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimerTriggerVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, TimerTriggerVisualizerComponent comp, ComponentInit args)
    {
        comp.PrimingAnimation = new Animation { Length = TimeSpan.MaxValue };
        {
            var flick = new AnimationTrackSpriteFlick();
            comp.PrimingAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = TriggerVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("primed", 0f));

            if (comp.CountdownSound != null)
            {
                var sound = new AnimationTrackPlaySound();
                comp.PrimingAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(comp.CountdownSound.GetSound(), 0));
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, TimerTriggerVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData(uid, TriggerVisuals.VisualState, out TriggerVisualState state, args.Component))
        {
            state = TriggerVisualState.Unprimed;
        }

        switch (state)
        {
            case TriggerVisualState.Primed:
                if (!AnimationSystem.HasRunningAnimation(uid, animPlayer, TimerTriggerVisualizerComponent.AnimationKey))
                {
                    AnimationSystem.Play(uid, animPlayer, comp.PrimingAnimation, TimerTriggerVisualizerComponent.AnimationKey);
                }
                break;
            case TriggerVisualState.Unprimed:
                args.Sprite.LayerSetState(0, "icon");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public enum TriggerVisualLayers : byte
{
    Base
}

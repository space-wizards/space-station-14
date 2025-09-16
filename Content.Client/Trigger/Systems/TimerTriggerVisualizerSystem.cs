using Content.Client.Trigger.Components;
using Content.Shared.Trigger;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Trigger.Systems;

public sealed class TimerTriggerVisualizerSystem : VisualizerSystem<TimerTriggerVisualsComponent>
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimerTriggerVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<TimerTriggerVisualsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PrimingAnimation = new Animation
        {
            Length = TimeSpan.MaxValue,
            AnimationTracks = {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = TriggerVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.PrimingSprite, 0f) }
                }
            },
        };

        if (ent.Comp.PrimingSound != null)
        {
            ent.Comp.PrimingAnimation.AnimationTracks.Add(
                new AnimationTrackPlaySound()
                {
                    KeyFrames = { new AnimationTrackPlaySound.KeyFrame(_audioSystem.ResolveSound(ent.Comp.PrimingSound), 0) }
                }
            );
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, TimerTriggerVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        || !TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData<TriggerVisualState>(uid, TriggerVisuals.VisualState, out var state, args.Component))
            state = TriggerVisualState.Unprimed;

        switch (state)
        {
            case TriggerVisualState.Primed:
                if (!AnimationSystem.HasRunningAnimation(uid, animPlayer, TimerTriggerVisualsComponent.AnimationKey))
                    AnimationSystem.Play((uid, animPlayer), comp.PrimingAnimation, TimerTriggerVisualsComponent.AnimationKey);
                break;
            case TriggerVisualState.Unprimed:
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), TriggerVisualLayers.Base, comp.UnprimedSprite);
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

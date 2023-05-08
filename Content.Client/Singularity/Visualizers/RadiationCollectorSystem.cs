using System;
using Content.Shared.Singularity.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Visualizers;

public sealed class RadiationCollectorSystem : VisualizerSystem<RadiationCollectorComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationCollectorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RadiationCollectorComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnComponentInit(EntityUid uid, RadiationCollectorComponent comp, ComponentInit args)
    {
        comp.ActivateAnimation = new Animation {
            Length = TimeSpan.FromSeconds(0.8f),
            AnimationTracks = {
                new AnimationTrackSpriteFlick() {
                    LayerKey = RadiationCollectorVisualLayers.Main,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(comp.ActivatingState, 0f)}
                }, // TODO: Make this play a sound when activating a radiation collector.
            }
        };

        comp.DeactiveAnimation = new Animation {
            Length = TimeSpan.FromSeconds(0.8f),
            AnimationTracks = {
                new AnimationTrackSpriteFlick() {
                    LayerKey = RadiationCollectorVisualLayers.Main,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(comp.DeactivatingState, 0f)}
                }, // TODO: Make this play a sound when deactivating a radiation collector.
            }
        };
    }

    private void OnAnimationCompleted(EntityUid uid, RadiationCollectorComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != RadiationCollectorComponent.AnimationKey
        ||  !AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state))
            return;

        switch(state)
        {
            case RadiationCollectorVisualState.Activating:
                AppearanceSystem.SetData(uid, RadiationCollectorVisuals.VisualState, RadiationCollectorVisualState.Active);
                break;
            case RadiationCollectorVisualState.Deactivating:
                AppearanceSystem.SetData(uid, RadiationCollectorVisuals.VisualState, RadiationCollectorVisualState.Deactive);
                break;
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, RadiationCollectorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        ||  !TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state, args.Component))
            state = RadiationCollectorVisualState.Deactive;

        switch (state)
        {
            case RadiationCollectorVisualState.Active:
                args.Sprite.LayerSetState(RadiationCollectorVisualLayers.Main, comp.ActiveState);
                break;
            case RadiationCollectorVisualState.Deactive:
                args.Sprite.LayerSetState(RadiationCollectorVisualLayers.Main, comp.InactiveState);
                break;
            
            // Neither of these animation actually work at the moment. The backend sets Active or Deactive without passing through these.
            case RadiationCollectorVisualState.Activating:
                if(!AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorComponent.AnimationKey))
                    AnimationSystem.Play(uid, animPlayer, comp.ActivateAnimation, RadiationCollectorComponent.AnimationKey);
                break;
            case RadiationCollectorVisualState.Deactivating:
                if(!AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorComponent.AnimationKey))
                    AnimationSystem.Play(uid, animPlayer, comp.DeactiveAnimation, RadiationCollectorComponent.AnimationKey);
                break;
        }
    }
}

public enum RadiationCollectorVisualLayers : byte
{
    Main
}

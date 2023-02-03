using System;
using Content.Shared.Singularity.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Visualizers;

public sealed class RadiationCollectorVisualizerSystem : VisualizerSystem<RadiationCollectorVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationCollectorVisualizerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RadiationCollectorVisualizerComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnInit(EntityUid uid, RadiationCollectorVisualizerComponent comp, ComponentInit args)
    {
        comp.ActivateAnimation = new Animation {Length = TimeSpan.FromSeconds(0.8f)};
        {
            var flick = new AnimationTrackSpriteFlick();
            comp.ActivateAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = RadiationCollectorVisualLayers.Main;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("ca_active", 0f));

            /*var sound = new AnimationTrackPlaySound();
            CloseAnimation.AnimationTracks.Add(sound);
            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));*/
        }

        comp.DeactiveAnimation = new Animation {Length = TimeSpan.FromSeconds(0.8f)};
        {
            var flick = new AnimationTrackSpriteFlick();
            comp.DeactiveAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = RadiationCollectorVisualLayers.Main;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("ca_deactive", 0f));

            /*var sound = new AnimationTrackPlaySound();
            CloseAnimation.AnimationTracks.Add(sound);
            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));*/
        }
    }

    private void OnAnimationCompleted(EntityUid uid, RadiationCollectorVisualizerComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != RadiationCollectorVisualizerComponent.AnimationKey)
            return;
        if(!AppearanceSystem.TryGetData(uid, RadiationCollectorVisuals.VisualState, out RadiationCollectorVisualState state))
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

    protected override void OnAppearanceChange(EntityUid uid, RadiationCollectorVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;
        if (!AppearanceSystem.TryGetData(uid, RadiationCollectorVisuals.VisualState, out RadiationCollectorVisualState state, args.Component))
        {
            state = RadiationCollectorVisualState.Deactive;
        }

        switch (state)
        {
            case RadiationCollectorVisualState.Active:
                args.Sprite.LayerSetState(RadiationCollectorVisualLayers.Main, "ca_on");
                break;
            case RadiationCollectorVisualState.Activating:
                if(!AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorVisualizerComponent.AnimationKey))
                    AnimationSystem.Play(uid, animPlayer, comp.ActivateAnimation, RadiationCollectorVisualizerComponent.AnimationKey);
                break;
            case RadiationCollectorVisualState.Deactivating:
                if(!AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorVisualizerComponent.AnimationKey))
                    AnimationSystem.Play(uid, animPlayer, comp.DeactiveAnimation, RadiationCollectorVisualizerComponent.AnimationKey);
                break;
            case RadiationCollectorVisualState.Deactive:
                args.Sprite.LayerSetState(RadiationCollectorVisualLayers.Main, "ca_off");
                break;
        }
    }
}

public enum RadiationCollectorVisualLayers : byte
{
    Main
}

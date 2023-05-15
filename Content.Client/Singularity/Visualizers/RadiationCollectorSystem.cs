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

    private void UpdateRadiationCollectorSpriteOrAnimation(EntityUid uid, RadiationCollectorVisualState state, RadiationCollectorComponent comp, SpriteComponent sprite, AnimationPlayerComponent? animPlayer = null)
    {
        if (state == comp.CurrentState)
            return;

        if ((RadiationCollectorVisualState) ((state ^ comp.CurrentState) & RadiationCollectorVisualState.Active) == RadiationCollectorVisualState.Active)
            state = (RadiationCollectorVisualState) (state | RadiationCollectorVisualState.Deactivating); // Convert to transition state.

        comp.CurrentState = state;

        switch (state)
        {
            case RadiationCollectorVisualState.Activating:
                if (!Resolve(uid, ref animPlayer, logMissing: false))
                    goto case RadiationCollectorVisualState.Active;
                AnimationSystem.Play(uid, animPlayer, comp.ActivateAnimation, RadiationCollectorComponent.AnimationKey);
                break;
            case RadiationCollectorVisualState.Deactivating:
                if (!Resolve(uid, ref animPlayer, logMissing: false))
                    goto case RadiationCollectorVisualState.Deactive;
                AnimationSystem.Play(uid, animPlayer, comp.DeactiveAnimation, RadiationCollectorComponent.AnimationKey);
                break;

            case RadiationCollectorVisualState.Active:
                sprite.LayerSetState(RadiationCollectorVisualLayers.Main, comp.ActiveState);
                break;
            case RadiationCollectorVisualState.Deactive:
                sprite.LayerSetState(RadiationCollectorVisualLayers.Main, comp.InactiveState);
                break;
        }
    }

    private void OnAnimationCompleted(EntityUid uid, RadiationCollectorComponent comp, AnimationCompletedEvent args)
    {
        SpriteComponent? sprite = null;
        if (args.Key != RadiationCollectorComponent.AnimationKey
        ||  !Resolve(uid, ref sprite))
            return;

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state))
            state = comp.CurrentState;

        // Convert to terminal state.
        state = (RadiationCollectorVisualState) (state & RadiationCollectorVisualState.Active);

        UpdateRadiationCollectorSpriteOrAnimation(uid, state, comp, sprite);
    }

    protected override void OnAppearanceChange(EntityUid uid, RadiationCollectorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        ||  !TryComp<AnimationPlayerComponent>(uid, out var animPlayer)
        ||  AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorComponent.AnimationKey))
            return;

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state, args.Component))
            state = RadiationCollectorVisualState.Deactive;

        UpdateRadiationCollectorSpriteOrAnimation(uid, state, comp, args.Sprite, animPlayer);
    }
}

public enum RadiationCollectorVisualLayers : byte
{
    Main
}

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

    private void UpdateVisuals(EntityUid uid, RadiationCollectorVisualState state, RadiationCollectorComponent comp, SpriteComponent sprite, AnimationPlayerComponent? animPlayer = null)
    {
        if (state == comp.CurrentState)
            return;
        if (!Resolve(uid, ref animPlayer))
            return;
        if (AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorComponent.AnimationKey))
            return;

        var targetState = (RadiationCollectorVisualState) (state & RadiationCollectorVisualState.Active);
        var destinationState = (RadiationCollectorVisualState) (comp.CurrentState & RadiationCollectorVisualState.Active);
        if (targetState != destinationState) // If where we're going is not where we want to be then we must go there next.
            targetState = (RadiationCollectorVisualState) (targetState | RadiationCollectorVisualState.Deactivating); // Convert to transition state.

        comp.CurrentState = state;

        switch (targetState)
        {
            case RadiationCollectorVisualState.Activating:
                AnimationSystem.Play(uid, animPlayer, comp.ActivateAnimation, RadiationCollectorComponent.AnimationKey);
                break;
            case RadiationCollectorVisualState.Deactivating:
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
        if (args.Key != RadiationCollectorComponent.AnimationKey)
            return;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        if (!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return; // Why doesn't AnimationCompletedEvent propagate the AnimationPlayerComponent? No idea, but it's in engine so I'm not touching it.

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state))
            state = comp.CurrentState;

        // Convert to terminal state.
        var targetState = (RadiationCollectorVisualState) (state & RadiationCollectorVisualState.Active);

        UpdateVisuals(uid, targetState, comp, sprite, animPlayer);
    }

    protected override void OnAppearanceChange(EntityUid uid, RadiationCollectorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state, args.Component))
            state = RadiationCollectorVisualState.Deactive;

        UpdateVisuals(uid, state, comp, args.Sprite, animPlayer);
    }
}

public enum RadiationCollectorVisualLayers : byte
{
    Main
}

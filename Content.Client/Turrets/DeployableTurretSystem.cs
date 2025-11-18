using Content.Client.Power;
using Content.Shared.Turrets;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Turrets;

public sealed partial class DeployableTurretSystem : SharedDeployableTurretSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeployableTurretComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DeployableTurretComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<DeployableTurretComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(Entity<DeployableTurretComponent> ent, ref ComponentInit args)
    {
        ent.Comp.DeploymentAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(ent.Comp.DeploymentLength),
            AnimationTracks = {
                new AnimationTrackSpriteFlick() {
                    LayerKey = DeployableTurretVisuals.Turret,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.DeployingState, 0f)}
                },
            }
        };

        ent.Comp.RetractionAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(ent.Comp.RetractionLength),
            AnimationTracks = {
                new AnimationTrackSpriteFlick() {
                    LayerKey = DeployableTurretVisuals.Turret,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.RetractingState, 0f)}
                },
            }
        };
    }

    private void OnAnimationCompleted(Entity<DeployableTurretComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != DeployableTurretComponent.AnimationKey)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_appearance.TryGetData<DeployableTurretState>(ent, DeployableTurretVisuals.Turret, out var state))
            state = ent.Comp.VisualState;

        // Convert to terminal state
        var targetState = state & DeployableTurretState.Deployed;

        UpdateVisuals(ent, targetState, sprite, args.AnimationPlayer);
    }

    private void OnAppearanceChange(Entity<DeployableTurretComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp<AnimationPlayerComponent>(ent, out var animPlayer))
            return;

        if (!_appearance.TryGetData<DeployableTurretState>(ent, DeployableTurretVisuals.Turret, out var state, args.Component))
            state = DeployableTurretState.Retracted;

        UpdateVisuals(ent, state, args.Sprite, animPlayer);
    }

    private void UpdateVisuals(Entity<DeployableTurretComponent> ent, DeployableTurretState state, SpriteComponent sprite, AnimationPlayerComponent? animPlayer = null)
    {
        if (!Resolve(ent, ref animPlayer))
            return;

        if (_animation.HasRunningAnimation(ent, animPlayer, DeployableTurretComponent.AnimationKey))
            return;

        var targetState = state & DeployableTurretState.Deployed;
        var destinationState = ent.Comp.VisualState & DeployableTurretState.Deployed;

        if (targetState != destinationState)
            targetState |= DeployableTurretState.Retracting;

        ent.Comp.VisualState = state;

        // Toggle layer visibility
        _sprite.LayerSetVisible((ent.Owner, sprite), DeployableTurretVisuals.Weapon, (targetState & DeployableTurretState.Deployed) > 0);
        _sprite.LayerSetVisible((ent.Owner, sprite), PowerDeviceVisualLayers.Powered, HasAmmo(ent) && targetState == DeployableTurretState.Retracted);

        // Change the visual state
        switch (targetState)
        {
            case DeployableTurretState.Deploying:
                _animation.Play((ent, animPlayer), (Animation)ent.Comp.DeploymentAnimation, DeployableTurretComponent.AnimationKey);
                break;

            case DeployableTurretState.Retracting:
                _animation.Play((ent, animPlayer), (Animation)ent.Comp.RetractionAnimation, DeployableTurretComponent.AnimationKey);
                break;

            case DeployableTurretState.Deployed:
                _sprite.LayerSetRsiState((ent.Owner, sprite), DeployableTurretVisuals.Turret, ent.Comp.DeployedState);
                break;

            case DeployableTurretState.Retracted:
                _sprite.LayerSetRsiState((ent.Owner, sprite), DeployableTurretVisuals.Turret, ent.Comp.RetractedState);
                break;
        }
    }
}

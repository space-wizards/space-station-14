using System.Numerics;
using Content.Client.Gravity;
using Content.Shared.Clown;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client.Clown;

public sealed class WaddleAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
        SubscribeLocalEvent<WaddleAnimationComponent, StartedWaddlingEvent>(OnStartedWalking);
        SubscribeLocalEvent<WaddleAnimationComponent, StoppedWaddlingEvent>(OnStoppedWalking);
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnMovementInput(EntityUid entity, WaddleAnimationComponent component, MoveInputEvent args)
    {
        // Prediction mitigation. Prediction means that MoveInputEvents are spammed repeatedly, even though you'd assume
        // they're once-only for the user actually doing something. As such do nothing if we're just repeating this FoR.
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (args.Component.HeldMoveButtons == MoveButtons.None && component.IsCurrentlyWaddling)
        {
            component.IsCurrentlyWaddling = false;

            RaiseLocalEvent(entity, new StoppedWaddlingEvent(entity));

            return;
        }

        if (component.IsCurrentlyWaddling)
            return;

        component.IsCurrentlyWaddling = true;

        RaiseLocalEvent(entity, new StartedWaddlingEvent(entity));
    }

    private void OnStartedWalking(EntityUid uid, WaddleAnimationComponent component, StartedWaddlingEvent args)
    {
        if (_animation.HasRunningAnimation(uid, component.KeyName))
        {
            return;
        }

        if (!TryComp<InputMoverComponent>(uid, out var mover))
        {
            return;
        }

        if (_gravity.IsWeightless(uid))
        {
            return;
        }

        var tumbleIntensity = component.LastStep ? 360 - component.TumbleIntensity : component.TumbleIntensity;
        var len = mover.Sprinting ? component.AnimationLength / 2 : component.AnimationLength;

        component.LastStep = !component.LastStep;
        component.IsCurrentlyWaddling = true;

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(len),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(tumbleIntensity), len/2),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), len/2),
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(), 0),
                        new AnimationTrackProperty.KeyFrame(component.HopIntensity, len/2),
                        new AnimationTrackProperty.KeyFrame(new Vector2(), len/2),
                    }
                }
            }
        };

        _animation.Play(uid, anim, component.KeyName);
    }

    private void OnStoppedWalking(EntityUid uid, WaddleAnimationComponent component, StoppedWaddlingEvent args)
    {
        _animation.Stop(uid, component.KeyName);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
        component.IsCurrentlyWaddling = false;
    }

    private void OnAnimationCompleted(EntityUid uid, WaddleAnimationComponent component, AnimationCompletedEvent args)
    {
        RaiseLocalEvent(uid, new StartedWaddlingEvent(uid));
    }
}

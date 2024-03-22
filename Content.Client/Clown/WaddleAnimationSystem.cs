using System.Numerics;
using Content.Client.Gravity;
using Content.Shared.Clown;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Clown;

public sealed class WaddleAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
        SubscribeLocalEvent<WaddleAnimationComponent, ClownStartedWalkingEvent>(OnStartedWalking);
        SubscribeLocalEvent<WaddleAnimationComponent, ClownStoppedWalkingEvent>(OnStoppedWalking);
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnMovementInput(EntityUid entity, WaddleAnimationComponent component, MoveInputEvent args)
    {
        if (args.HasDirectionalMovement)
        {
            RaiseLocalEvent(entity, new ClownStartedWalkingEvent(entity));

            return;
        }

        RaiseLocalEvent(entity, new ClownStoppedWalkingEvent(entity));
    }

    private void OnStartedWalking(EntityUid uid, WaddleAnimationComponent component, ClownStartedWalkingEvent args)
    {
        if (_animation.HasRunningAnimation(uid, component.KeyName))
        {
            return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite))
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
        var len = mover.Sprinting ? component.AnimationLength/2 : component.AnimationLength;

        component.LastStep = !component.LastStep;

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(len+0.01),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0.01f),
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
                        new AnimationTrackProperty.KeyFrame(new Vector2(), 0.01f),
                        new AnimationTrackProperty.KeyFrame(component.HopIntensity, len/2),
                        new AnimationTrackProperty.KeyFrame(new Vector2(), len/2),
                    }
                }
            }
        };

        _animation.Play(uid, anim, component.KeyName);
    }

    private void OnStoppedWalking(EntityUid uid, WaddleAnimationComponent component, ClownStoppedWalkingEvent args)
    {
        _animation.Stop(uid, component.KeyName);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
    }

    private void OnAnimationCompleted(EntityUid uid, WaddleAnimationComponent component, AnimationCompletedEvent args)
    {
        RaiseLocalEvent(uid, new ClownStartedWalkingEvent(uid));
    }
}

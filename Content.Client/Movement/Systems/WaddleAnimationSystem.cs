using System.Numerics;
using Content.Client.Buckle;
using Content.Client.Gravity;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class WaddleAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
        SubscribeLocalEvent<WaddleAnimationComponent, StartedWaddlingEvent>(OnStartedWalking);
        SubscribeLocalEvent<WaddleAnimationComponent, StoppedWaddlingEvent>(OnStoppedWalking);
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<WaddleAnimationComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<WaddleAnimationComponent, KnockedDownEvent>(OnKnockedDown);
        SubscribeLocalEvent<WaddleAnimationComponent, BuckleChangeEvent>(OnBuckleChange);
    }

    private void OnMovementInput(EntityUid entity, WaddleAnimationComponent component, MoveInputEvent args)
    {
        // Prediction mitigation. Prediction means that MoveInputEvents are spammed repeatedly, even though you'd assume
        // they're once-only for the user actually doing something. As such do nothing if we're just repeating this FoR.
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!args.HasDirectionalMovement && component.IsCurrentlyWaddling)
        {
            var stopped = new StoppedWaddlingEvent(entity);

            RaiseLocalEvent(entity, ref stopped);

            return;
        }

        // Only start waddling if we're not currently AND we're actually moving.
        if (component.IsCurrentlyWaddling || !args.HasDirectionalMovement)
            return;

        var started = new StartedWaddlingEvent(entity);

        RaiseLocalEvent(entity, ref started);
    }

    private void OnStartedWalking(EntityUid uid, WaddleAnimationComponent component, StartedWaddlingEvent args)
    {
        if (_animation.HasRunningAnimation(uid, component.KeyName))
            return;

        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return;

        if (_gravity.IsWeightless(uid))
            return;


        if (!_actionBlocker.CanMove(uid, mover))
            return;

        // Do nothing if buckled in
        if (_buckle.IsBuckled(uid))
            return;

        // Do nothing if crit or dead (for obvious reasons)
        if (_mobState.IsIncapacitated(uid))
            return;

        var tumbleIntensity = component.LastStep ? 360 - component.TumbleIntensity : component.TumbleIntensity;
        var len = mover.Sprinting ? component.AnimationLength * component.RunAnimationLengthMultiplier : component.AnimationLength;

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
        StopWaddling(uid, component);
    }

    private void OnAnimationCompleted(EntityUid uid, WaddleAnimationComponent component, AnimationCompletedEvent args)
    {
        var started = new StartedWaddlingEvent(uid);

        RaiseLocalEvent(uid, ref started);
    }

    private void OnStunned(EntityUid uid, WaddleAnimationComponent component, StunnedEvent args)
    {
        StopWaddling(uid, component);
    }

    private void OnKnockedDown(EntityUid uid, WaddleAnimationComponent component, KnockedDownEvent args)
    {
        StopWaddling(uid, component);
    }

    private void OnBuckleChange(EntityUid uid, WaddleAnimationComponent component, BuckleChangeEvent args)
    {
        StopWaddling(uid, component);
    }

    private void StopWaddling(EntityUid uid, WaddleAnimationComponent component)
    {
        if (!component.IsCurrentlyWaddling)
            return;

        _animation.Stop(uid, component.KeyName);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);

        component.IsCurrentlyWaddling = false;
    }
}

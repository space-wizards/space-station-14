using System.Numerics;
using Content.Client.Buckle;
using Content.Client.Gravity;
using Content.Shared.ActionBlocker;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Movement.Systems;

public sealed class ClientWaddleAnimationSystem : SharedWaddleAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Start waddling
        SubscribeAllEvent<StartedWaddlingEvent>((msg, args) =>
        {
            if (TryComp<WaddleAnimationComponent>(GetEntity(msg.Entity), out var comp))
                StartWaddling(GetEntity(msg.Entity), comp);
        });

        // Handle concluding animations
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);

        // Stop waddling
        SubscribeAllEvent<StoppedWaddlingEvent>((msg, args) =>
        {
            if (TryComp<WaddleAnimationComponent>(GetEntity(msg.Entity), out var comp))
                StopWaddling(GetEntity(msg.Entity), comp);
        });
    }

    private void StartWaddling(EntityUid uid, WaddleAnimationComponent component)
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

        PlayWaddleAnimationUsing(uid, component, CalculateAnimationLength(component, mover), CalculateTumbleIntensity(component));
    }

    private static float CalculateTumbleIntensity(WaddleAnimationComponent component)
    {
        return component.LastStep ? 360 - component.TumbleIntensity : component.TumbleIntensity;
    }

    private static float CalculateAnimationLength(WaddleAnimationComponent component, InputMoverComponent mover)
    {
        return mover.Sprinting ? component.AnimationLength * component.RunAnimationLengthMultiplier : component.AnimationLength;
    }

    private void OnAnimationCompleted(EntityUid uid, WaddleAnimationComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != component.KeyName)
            return;

        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return;

        PlayWaddleAnimationUsing(uid, component, CalculateAnimationLength(component, mover), CalculateTumbleIntensity(component));
    }

    private void StopWaddling(EntityUid uid, WaddleAnimationComponent component)
    {
        if (!_animation.HasRunningAnimation(uid, component.KeyName))
            return;

        _animation.Stop(uid, component.KeyName);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
    }

    private void PlayWaddleAnimationUsing(EntityUid uid, WaddleAnimationComponent component, float len, float tumbleIntensity)
    {
        component.LastStep = !component.LastStep;

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
}

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

public sealed class WaddleAnimationSystem : SharedWaddleAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<StartedWaddlingEvent>(OnStartWaddling);
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeAllEvent<StoppedWaddlingEvent>(OnStopWaddling);
    }

    private void OnStartWaddling(StartedWaddlingEvent msg, EntitySessionEventArgs args)
    {
        if (TryComp<WaddleAnimationComponent>(GetEntity(msg.Entity), out var comp))
            StartWaddling((GetEntity(msg.Entity), comp));
    }

    private void OnStopWaddling(StoppedWaddlingEvent msg, EntitySessionEventArgs args)
    {
        if (TryComp<WaddleAnimationComponent>(GetEntity(msg.Entity), out var comp))
            StopWaddling((GetEntity(msg.Entity), comp));
    }

    private void StartWaddling(Entity<WaddleAnimationComponent> entity)
    {
        if (_animation.HasRunningAnimation(entity.Owner, entity.Comp.KeyName))
            return;

        if (!TryComp<InputMoverComponent>(entity.Owner, out var mover))
            return;

        if (_gravity.IsWeightless(entity.Owner))
            return;

        if (!_actionBlocker.CanMove(entity.Owner, mover))
            return;

        // Do nothing if buckled in
        if (_buckle.IsBuckled(entity.Owner))
            return;

        // Do nothing if crit or dead (for obvious reasons)
        if (_mobState.IsIncapacitated(entity.Owner))
            return;

        PlayWaddleAnimationUsing(
            (entity.Owner, entity.Comp),
            CalculateAnimationLength(entity.Comp, mover),
            CalculateTumbleIntensity(entity.Comp)
        );
    }

    private static float CalculateTumbleIntensity(WaddleAnimationComponent component)
    {
        return component.LastStep ? 360 - component.TumbleIntensity : component.TumbleIntensity;
    }

    private static float CalculateAnimationLength(WaddleAnimationComponent component, InputMoverComponent mover)
    {
        return mover.Sprinting ? component.AnimationLength * component.RunAnimationLengthMultiplier : component.AnimationLength;
    }

    private void OnAnimationCompleted(Entity<WaddleAnimationComponent> entity, ref AnimationCompletedEvent args)
    {
        if (args.Key != entity.Comp.KeyName)
            return;

        if (!TryComp<InputMoverComponent>(entity.Owner, out var mover))
            return;

        PlayWaddleAnimationUsing(
            (entity.Owner, entity.Comp),
            CalculateAnimationLength(entity.Comp, mover),
            CalculateTumbleIntensity(entity.Comp)
        );
    }

    private void StopWaddling(Entity<WaddleAnimationComponent> entity)
    {
        if (!_animation.HasRunningAnimation(entity.Owner, entity.Comp.KeyName))
            return;

        _animation.Stop(entity.Owner, entity.Comp.KeyName);

        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
    }

    private void PlayWaddleAnimationUsing(Entity<WaddleAnimationComponent> entity, float len, float tumbleIntensity)
    {
        entity.Comp.LastStep = !entity.Comp.LastStep;

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
                        new AnimationTrackProperty.KeyFrame(entity.Comp.HopIntensity, len/2),
                        new AnimationTrackProperty.KeyFrame(new Vector2(), len/2),
                    }
                }
            }
        };

        _animation.Play(entity.Owner, anim, entity.Comp.KeyName);
    }
}

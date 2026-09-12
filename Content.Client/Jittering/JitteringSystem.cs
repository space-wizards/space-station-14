using System.Numerics;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Jittering;

/// <inheritdoc />
public sealed partial class JitteringSystem : SharedJitteringSystem
{
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [Dependency] private EntityQuery<AnimationPlayerComponent> _animationQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<StatusEffectComponent> _statusQuery = default!;

    private const string JitterAnimationKey = "jittering";

    // When jittering stops, a final animation is played using this
    private const float ReturnSpeed = 0.35f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);

        SubscribeLocalEvent<JitteringComponent, AnimationCompletedEvent>(OnAnimationComplete);
        SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectRelayedEvent<AnimationCompletedEvent>>(OnRelayAnimationCompleted);
    }

    #region Subscriptions

    // Start the animation
    private void OnStatusApplied(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        StartJitter(args.Target, ent.Comp.Jitter);
    }

    private void OnAnimationComplete(Entity<JitteringComponent> ent, ref AnimationCompletedEvent args)
    {
        // Ideally this is all handled by StatusEffectRemovedEvent and a generic relay for AnimationCompletedEvent.
        // However, container prediction occasionally removes the status from its container, raising the event.
        // This is the only thing absolutely requiring the existence of JitteringComponent.

        if (args.Key != JitterAnimationKey)
            return;

        // If we still have a jitter status, relay it and let them handle the animation
        if (_statusEffects.HasEffectComp<JitteringStatusEffectComponent>(ent))
        {
            var effectContainerComp = EnsureComp<StatusEffectContainerComponent>(ent);
            _statusEffects.RelayEvent((ent, effectContainerComp), args);
            return;
        }

        // If we have no jitter status, end the jitter animation and take us home
        if (!_animationQuery.TryComp(ent, out var player)
            || !_spriteQuery.TryComp(ent, out var sprite))
            return;

        _animation.Stop((ent, player), JitterAnimationKey);
        _animation.Play((ent, player),
                        GetLineAnimation(sprite.Offset, ent.Comp.StartOffset, ReturnSpeed),
                        JitterAnimationKey);

        // Not deferred. Nothing else should care about this component
        RemComp<JitteringComponent>(ent);
    }

    // Repeat the animation
    private void OnRelayAnimationCompleted(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AnimationCompletedEvent> args)
    {
        if (args.Args.Key == JitterAnimationKey)
            StartJitter(args.Args.Uid, ent.Comp.Jitter);
    }

    #endregion
    #region Helpers

    /// <inheritdoc/>
    [PublicAPI]
    public override void AdjustJitter(EntityUid statusEnt, JitterParameters jitter)
    {
        base.AdjustJitter(statusEnt, jitter);

        if (_statusQuery.TryComp(statusEnt, out var status) && status.AppliedTo != null)
            StartJitter(status.AppliedTo.Value, jitter);
    }

    /// <summary>
    /// Starts a jitter animation on an entity if it doesn't already have an existing jitter.
    /// </summary>
    /// <param name="target">The entity to start jittering.</param>
    /// <param name="jitter">What kind of jitter to apply.</param>
    private void StartJitter(EntityUid target, JitterParameters jitter)
    {
        if (_animation.HasRunningAnimation(target, JitterAnimationKey))
            return; // If we're already playing a jitter don't worry about it
                    // Maybe we can find a way to combine all jitter settings instead

        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        // WARNING: Status effects sin
        // Status effects should not be applying components to entities, everything should go through relays.
        // Because of difficulties with animations we have to remember where our sprite offset started to return it,
        // and because we can have multiple status effects they need to share that offset somewhere.
        // Resolving the issue of container prediction falsely calling StatusEffectRemovedEvent can allow us to share
        // this offset on the statuses themselves, but ideally change to animations save us from needing to remember it at all.
        if (!EnsureComp<JitteringComponent>(target, out var jitterComp))
            jitterComp.StartOffset = spriteComp.Offset;

        var playerComp = EnsureComp<AnimationPlayerComponent>(target);
        _animation.Play((target, playerComp),
                                GetJitterAnimation(jitter, spriteComp.Offset, jitterComp.StartOffset),
                                JitterAnimationKey);
    }

    /// <summary>
    /// Unwraps <c>JitterParameters</c> and returns a new sprite animation.
    /// </summary>
    private Animation GetJitterAnimation(JitterParameters jitter, Vector2 currentOffset, Vector2 origin)
    {
        if (jitter.Frequency <= 0) // Preempt divide by 0 and strange animation durations
            return new Animation();

        var newOffset = _random.NextVector2(jitter.MinRadius, jitter.MaxRadius);
        newOffset = Vector2.Transform(newOffset, jitter.Matrix);

        // If we're in the same quadrant as our current location, invert the offset
        // Reduces repetitive behavior and increases large movements
        if (jitter.MatrixT == Vector2.Zero
            && Math.Sign(newOffset.X) == Math.Sign(currentOffset.X - origin.X)
            && Math.Sign(newOffset.Y) == Math.Sign(currentOffset.Y - origin.Y))
        {
            newOffset = -newOffset;
        }

        var length = 1f / jitter.Frequency;

        switch (jitter.Type)
        {
            case JitterType.Line:
                return GetLineAnimation(currentOffset, origin + newOffset, length);

            case JitterType.Arch:
                return GetArchAnimation(currentOffset, origin + newOffset, length);

            default:
                return new Animation();
        }
    }

    /// <summary>
    /// Returns a simple lerp between two points.
    /// </summary>
    private static Animation GetLineAnimation(Vector2 current, Vector2 destination, float length)
    {
        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(current, 0f),
                        new AnimationTrackProperty.KeyFrame(destination, length),
                    },
                },
            },
        };
    }

    /// <summary>
    /// Returns a lerp between three points where the midpoint is largest on the Y axis.
    /// </summary>
    private static Animation GetArchAnimation(Vector2 current, Vector2 destination, float length)
    {
        // We add the differance of our two Y's to create a new Y for the midpoint.
        // This ensures we're at the highest Y while keeping the result relative to the existing vertical movement.
        var midpoint = (current + destination) / 2;
        midpoint.Y += Math.Abs(current.Y - destination.Y);

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(current, 0f),
                        new AnimationTrackProperty.KeyFrame(midpoint, length / 2f),
                        new AnimationTrackProperty.KeyFrame(destination, length),
                    },
                },
            },
        };
    }

    #endregion
}

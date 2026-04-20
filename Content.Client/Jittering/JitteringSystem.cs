using System.Numerics;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Jittering;

/// <inheritdoc />
public sealed class JitteringSystem : SharedJitteringSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    [Dependency] private readonly EntityQuery<AnimationPlayerComponent> _animationQuery = default!;
    [Dependency] private readonly EntityQuery<JitteringComponent> _jitterQuery = default!;
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

    private readonly string _jitterAnimationKey = "jittering";
    private readonly string _jitterReturnAnimationKey = "jitteringReturn";

    private const float ReturnSpeed = 0.2f;

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

    // This could be handled by StatusEffectRemovedEvent and a generic relay for AnimationCompletedEvent,
    // But container prediction was making it difficult without a marker component
    private void OnAnimationComplete(Entity<JitteringComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != _jitterAnimationKey)
            return;

        // If we still have a jitter status, relay it and let them handle the jitter
        if (_statusEffects.HasEffectComp<JitteringStatusEffectComponent>(ent))
        {
            var effectContainerComp = EnsureComp<StatusEffectContainerComponent>(ent);
            _statusEffects.RelayEvent((ent, effectContainerComp), args);
            return;
        }

        // If we have no jitter status, end the jitter animation and take us home
        if (!_animationQuery.TryComp(ent, out var player)
            || !_spriteQuery.TryComp(ent, out var sprite)
            || !_jitterQuery.TryComp(ent, out var jitter))
            return;

        _animation.Stop((ent, player), _jitterAnimationKey);
        _animation.Play((ent, player),
                        GetLineAnimation(sprite.Offset, jitter.StartOffset, ReturnSpeed),
                        _jitterReturnAnimationKey);

        RemCompDeferred<JitteringComponent>(ent);
    }

    // Repeat the animation
    private void OnRelayAnimationCompleted(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AnimationCompletedEvent> args)
    {
        if (args.Args.Key == _jitterAnimationKey)
            StartJitter(args.Args.Uid, ent.Comp.Jitter);
    }

    #endregion
    #region Helpers

    /// <summary>
    /// Starts a jitter animation on an entity if it doesn't already have an existing jitter.
    /// </summary>
    /// <param name="target">The entity to start jittering.</param>
    /// <param name="jitter">What kind of jitter to apply.</param>
    private void StartJitter(EntityUid target, JitterParameters jitter)
    {
        if (_animation.HasRunningAnimation(target, _jitterAnimationKey))
            return; // If we're already playing a jitter don't worry about it
                    // Maybe we can find a way to combine all jitter settings instead

        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        if (!EnsureComp<JitteringComponent>(target, out var jitterComp))
            jitterComp.StartOffset = spriteComp.Offset;

        var playerComp = EnsureComp<AnimationPlayerComponent>(target);
        _animation.Play((target, playerComp),
                                GetJitterAnimation(jitter, spriteComp.Offset, jitterComp.StartOffset),
                                _jitterAnimationKey);
    }

    /// <summary>
    /// Unwraps <c>JitterParameters</c> and returns a new sprite animation between two points.
    /// </summary>
    private Animation GetJitterAnimation(JitterParameters jitter, Vector2 currentOffset, Vector2 origin)
    {
        if (jitter.Frequency <= 0) // Preempt divide by 0 and strange animation durations
        {
            Log.Warning($"Attempted to start a jitter animation with a frequency of 0 or less. Frequency was: {jitter.Frequency}");
            return new Animation();
        }

        var newOffset = _random.NextVector2(jitter.MinRadius, jitter.MaxRadius);

        // If we're in the same quadrant as our current location, invert the offset
        // Reduces repetitive behavior and increases large movements, but breaks if the matrix has a translation
        if (Math.Sign(newOffset.X) == Math.Sign(currentOffset.X)
            && Math.Sign(newOffset.Y) == Math.Sign(currentOffset.Y)
            && jitter.MatrixT == Vector2.Zero)
        {
            newOffset = -newOffset;
        }

        newOffset = Vector2.Transform(newOffset, jitter.Matrix);
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
    /// Returns a lerp between three points, with its midpoint always being the largest on the Y axis.
    /// </summary>
    private static Animation GetArchAnimation(Vector2 current, Vector2 destination, float length)
    {
        var midpoint = (current + destination) / 2;
        midpoint.Y = Math.Max(current.Y, destination.Y) + Math.Abs(current.Y - destination.Y);

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
                        new AnimationTrackProperty.KeyFrame(midpoint, length / 2),
                        new AnimationTrackProperty.KeyFrame(destination, length),
                    },
                },
            },
        };
    }

    #endregion
}

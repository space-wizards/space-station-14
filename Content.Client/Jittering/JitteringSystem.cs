using System.Numerics;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

// todo namespace
namespace Content.Client.Jittering
{
    /// <inheritdoc />
    public sealed class JitteringSystem : SharedJitteringSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AnimationPlayerSystem _animation = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

        [Dependency] private readonly EntityQuery<AnimationPlayerComponent> _animationQuery = default!;
        [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

        private readonly string _jitterAnimationKey = "jittering";
        private readonly string _jitterReturnAnimationKey = "jitteringReturn";

        // The least of all evils when dealing with resetting the sprite after we're done jittering
        // Ideally animations use deltas instead of setters so we don't need to worry about resetting it
        private static readonly Vector2 SpriteOrigin = Vector2.Zero;

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
                || !_spriteQuery.TryComp(ent, out var sprite))
                return;

            _animation.Stop((ent, player), _jitterAnimationKey);
            _animation.Play((ent, player), GetReturnAnimation(sprite.Offset, SpriteOrigin), _jitterReturnAnimationKey);

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

            EnsureComp<JitteringComponent>(target);

            var playerComp = EnsureComp<AnimationPlayerComponent>(target);
            _animation.Play((target, playerComp),
                                    GetJitterAnimation(jitter, spriteComp.Offset, SpriteOrigin),
                                    _jitterAnimationKey);
        }

        /// <returns>
        /// A lerp between <c>currentOffset</c> and a position derived from <c>origin</c> and <c>jitter</c>.
        /// </returns>
        private Animation GetJitterAnimation(JitterParameters jitter, Vector2 currentOffset, Vector2 origin)
        {
            var newOffset = _random.NextVector2(jitter.MinRadius, jitter.MaxRadius);

            // If we're in the same quadrant as our current location, invert the offset
            // Reduces repetitive behavior and increases large movements
            if (Math.Sign(newOffset.X) == Math.Sign(currentOffset.X)
                && Math.Sign(newOffset.Y) == Math.Sign(currentOffset.Y))
            {
                newOffset = -newOffset;
            }

            newOffset = Vector2.Transform(newOffset, jitter.Matrix);

            // avoid dividing by 0 so animations don't try to be infinitely long
            var length = jitter.Frequency <= 0 ? 0f : 1f / jitter.Frequency;

            // create and play the animation
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
                            new AnimationTrackProperty.KeyFrame(currentOffset, 0f),
                            new AnimationTrackProperty.KeyFrame(origin + newOffset, length),
                        },
                    },
                },
            };
        }

        /// <returns>A simple lerp from <c>currentOffset</c> to <c>origin</c>.</returns>
        private static Animation GetReturnAnimation(Vector2 currentOffset, Vector2 origin, float returnSpeed = 0.2f)
        {
            // create and play the animation
            return new Animation()
            {
                Length = TimeSpan.FromSeconds(returnSpeed),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Offset),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(currentOffset, 0f),
                            new AnimationTrackProperty.KeyFrame(origin, returnSpeed),
                        },
                    },
                },
            };
        }

        #endregion
    }
}

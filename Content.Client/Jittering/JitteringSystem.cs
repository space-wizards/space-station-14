using System.Numerics;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
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
        [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

        [Dependency] private readonly EntityQuery<AnimationPlayerComponent> _animationQuery = default!;
        [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

        private readonly string _jitterAnimationKey = "jittering";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<JitteringComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<JitteringComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<JitteringComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        }

        // Start the animation
        private void OnStartup(Entity<JitteringComponent> ent, ref ComponentStartup args)
        {
            if (!_spriteQuery.TryComp(ent, out var sprite))
                return;

            var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent);

            if (!TryGetCombinedStatusJitters(ent, out var newJitter))
                return;

            ent.Comp.StartOffset = sprite.Offset;
            DoJitter((ent.Owner, ent.Comp, sprite, animationPlayer), newJitter);
        }

        // End the animation
        private void OnShutdown(Entity<JitteringComponent> ent, ref ComponentShutdown args)
        {
            if (_animationQuery.TryComp(ent, out var animationPlayer))
                _animationPlayer.Stop(ent, animationPlayer, _jitterAnimationKey);

            if (_spriteQuery.TryComp(ent, out var sprite))
                _sprite.SetOffset((ent, sprite), sprite.Offset - ent.Comp.StartOffset);
        }

        // repeat the animation
        private void OnAnimationCompleted(Entity<JitteringComponent> ent, ref AnimationCompletedEvent args)
        {
            if (args.Key != _jitterAnimationKey)
                return;

            // If the animation ended prematurely it's probably because of ComponentShutdown
            if (!args.Finished)
                return;

            if (TryGetCombinedStatusJitters(ent, out var newJitter)
                && _spriteQuery.TryComp(ent, out var sprite)
                && _animationQuery.TryComp(ent, out var animationPlayer))
            {
                DoJitter((ent.Owner, ent.Comp, sprite, animationPlayer), newJitter);
            }
        }

        /// <summary>
        /// Creates a jitter animation, then plays the animation on the entity.
        /// </summary>
        private void DoJitter(Entity<JitteringComponent, SpriteComponent, AnimationPlayerComponent> ent, JitterParams newJitter)
        {
            var (uid, jittering, sprite, animationPlayer) = ent;

            // Create a random offset
            var offset = _random.NextVector2(newJitter.MinRadius, newJitter.MaxRadius);
            offset = Vector2.Transform(offset, Matrix3x2.Create(newJitter.XSheer, newJitter.YSheer, Vector2.Zero));

            // If we're in the same quadrant as our last location, invert the offset
            // Reduces repetitive behavior and increases large movements
            if (Math.Sign(offset.X) == Math.Sign(jittering.LastJitter.X)
                && Math.Sign(offset.Y) == Math.Sign(jittering.LastJitter.Y))
            {
                offset = -offset;
            }

            jittering.LastJitter = offset;

            // avoid dividing by 0 so animations don't try to be infinitely long
            var length = newJitter.Frequency <= 0 ? 0f : 1f / newJitter.Frequency;

            // create and play the animation
            var animation = new Animation()
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
                            new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                            new AnimationTrackProperty.KeyFrame(jittering.StartOffset + offset, length),
                        }
                    }
                }
            };
            _animationPlayer.Play((uid, animationPlayer), animation, _jitterAnimationKey);
        }
    }
}

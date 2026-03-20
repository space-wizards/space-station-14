using System.Numerics;
using Content.Shared.Jittering;
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

            ent.Comp.StartOffset = sprite.Offset;
            _animationPlayer.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), _jitterAnimationKey);
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

            if (_animationQuery.TryComp(ent, out var animationPlayer)
                && _spriteQuery.TryComp(ent, out var sprite))
                _animationPlayer.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), _jitterAnimationKey);
        }

        /// <summary>
        /// Creates and returns the animation to play on the sprite.
        /// </summary>
        private Animation GetAnimation(JitteringComponent jittering, SpriteComponent sprite)
        {
            // Create a random offset
            var offset = jittering.Amplitude * _random.NextVector2(jittering.MinRadius, jittering.MaxRadius);

            // If we're in the same quadrant as our last location, invert the offset
            // Reduces repetitive behavior and increases large movements
            if (Math.Sign(offset.X) == Math.Sign(jittering.LastJitter.X)
                && Math.Sign(offset.Y) == Math.Sign(jittering.LastJitter.Y))
            {
                offset = -offset;
            }

            // Hack together a matrix because there's no node validator for Matrix3x2
            // Apply it to the offset to create an oval (or line) of potential destinations
            var matrix = Matrix3x2.Create(jittering.XSheer, jittering.YSheer, Vector2.Zero);
            offset = Vector2.Transform(offset, matrix);

            if (jittering.Frequency == 0)
                return new Animation();

            // avoid dividing by 0 so animations don't try to be infinitely long
            var length = jittering.Frequency <= 0 ? 0f : 1f / jittering.Frequency;

            var ani = new Animation()
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
                            // Start at our current location
                            new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                            // Subtract the previous offset from our current location, then add the new offset
                            // this is still bulldozing fuck
                            new AnimationTrackProperty.KeyFrame(sprite.Offset - jittering.LastJitter + offset, length),
                        }
                    }
                }
            };

            jittering.LastJitter = offset;
            return ani;
        }
    }
}

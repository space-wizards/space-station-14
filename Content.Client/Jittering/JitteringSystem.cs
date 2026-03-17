using System.Numerics;
using Content.Shared.Jittering;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

// todo namespace
namespace Content.Client.Jittering
{
    public sealed class JitteringSystem : SharedJitteringSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

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
            if (!TryComp<SpriteComponent>(ent, out var sprite))
                return;

            var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent);

            ent.Comp.StartOffset = sprite.Offset;
            _animationPlayer.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), _jitterAnimationKey);
        }

        // End the animation
        private void OnShutdown(Entity<JitteringComponent> ent, ref ComponentShutdown args)
        {
            if (TryComp<AnimationPlayerComponent>(ent, out var animationPlayer))
                _animationPlayer.Stop(ent, animationPlayer, _jitterAnimationKey);

            if (TryComp<SpriteComponent>(ent, out var sprite))
                _sprite.SetOffset((ent, sprite), ent.Comp.StartOffset);
        }

        // repeat the animation
        private void OnAnimationCompleted(Entity<JitteringComponent> ent, ref AnimationCompletedEvent args)
        {
            if (args.Key != _jitterAnimationKey)
                return;

            if (!args.Finished)
                return;

            if (TryComp<AnimationPlayerComponent>(ent, out var animationPlayer)
                && TryComp<SpriteComponent>(ent, out var sprite))
                _animationPlayer.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), _jitterAnimationKey);
        }

        /// <summary>
        /// Creates and returns the animation to play on the sprite.
        /// </summary>
        private Animation GetAnimation(JitteringComponent jittering, SpriteComponent sprite)
        {
            var offset = _random.NextVector2(jittering.Radius);

            // If we're in the same quadrant as our last location, invert the offset
            // Reduces repetitive behavior and increases large movements
            if (Math.Sign(offset.X) == Math.Sign(jittering.LastJitter.X)
                && Math.Sign(offset.Y) == Math.Sign(jittering.LastJitter.Y))
            {
                offset = -offset;
            }

            jittering.LastJitter = offset;

            var length = 0f;
            // avoid dividing by 0 so animations don't try to be infinitely long
            if (jittering.Frequency > 0)
                length = 1f / jittering.Frequency;

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
                            new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                            new AnimationTrackProperty.KeyFrame(jittering.StartOffset + offset, length),
                        }
                    }
                }
            };
        }
    }
}

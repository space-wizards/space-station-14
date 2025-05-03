using System.Numerics;
using Content.Shared.Stunnable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using TerraFX.Interop.Xlib;

namespace Content.Client.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        [Dependency] private readonly AnimationPlayerSystem _animation = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private readonly int[] _sign = [-1, 1];
        private const string StunnedAnimationKey = "stunned";
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunnedComponent, StunnedEvent>(OnStunned);
            SubscribeLocalEvent<StunnedComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        }

        private void OnStunned(Entity<StunnedComponent> ent, ref StunnedEvent args)
        {
            if (!TryComp<SpriteComponent>(ent, out var sprite) || !_timing.IsFirstTimePredicted)
                return;

            var animation = GetAnimation(ent.Comp, sprite);

            _animation.Play(ent.Owner, animation, StunnedAnimationKey);
        }

        private void OnStunnedShutdown(Entity<StunnedComponent> ent, ref ComponentShutdown args)
        {
            if (!TryComp<AnimationPlayerComponent>(ent, out var animation))
                return;

            _animation.Stop(ent.Owner, animation, StunnedAnimationKey);
        }

        private void OnAnimationCompleted(Entity<StunnedComponent> ent, ref AnimationCompletedEvent args)
        {
            if(args.Key != StunnedAnimationKey)
                return;

            if (!args.Finished)
                return;

            if (TryComp(ent, out AnimationPlayerComponent? animationPlayer)
                && TryComp(ent, out SpriteComponent? sprite))
                _animation.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), StunnedAnimationKey);
        }

        private Animation GetAnimation(StunnedComponent stunned, SpriteComponent sprite)
        {
            var amplitude = MathF.Min(0.4f, stunned.Amplitude);
            var offset = new Vector2(_random.NextFloat(amplitude/4f, amplitude),
                _random.NextFloat(amplitude / 4f, amplitude / 3f));

            offset.X *= _random.Pick(_sign);
            offset.Y *= _random.Pick(_sign);

            if (Math.Sign(offset.X) == Math.Sign(stunned.LastJitter.X)
                || Math.Sign(offset.Y) == Math.Sign(stunned.LastJitter.Y))
            {
                // If the sign is the same as last time on both axis we flip one randomly
                // to avoid jitter staying in one quadrant too much.
                if (_random.Prob(0.5f))
                    offset.X *= -1;
                else
                    offset.Y *= -1;
            }

            var length = 0f;
            // avoid dividing by 0 so animations don't try to be infinitely long
            if (stunned.Frequency > 0)
                length = 1f / stunned.Frequency;

            stunned.LastJitter = offset;

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
                            new AnimationTrackProperty.KeyFrame(stunned.StartOffset + offset, length),
                        }
                    }
                }
            };
        }
    }
}

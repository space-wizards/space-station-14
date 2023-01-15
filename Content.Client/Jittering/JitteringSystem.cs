using System;
using System.Collections.Immutable;
using Content.Shared.Jittering;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Client.Jittering
{
    public sealed class JitteringSystem : SharedJitteringSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly float[] _sign = { -1, 1 };
        private readonly string _jitterAnimationKey = "jittering";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<JitteringComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<JitteringComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<JitteringComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        }

        private void OnStartup(EntityUid uid, JitteringComponent jittering, ComponentStartup args)
        {
            if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                return;

            var animationPlayer = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);

            animationPlayer.Play(GetAnimation(jittering, sprite), _jitterAnimationKey);
        }

        private void OnShutdown(EntityUid uid, JitteringComponent jittering, ComponentShutdown args)
        {
            if (EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? animationPlayer))
                animationPlayer.Stop(_jitterAnimationKey);

            if (EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                sprite.Offset = Vector2.Zero;
        }

        private void OnAnimationCompleted(EntityUid uid, JitteringComponent jittering, AnimationCompletedEvent args)
        {
            if(args.Key != _jitterAnimationKey)
                return;

            if(EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? animationPlayer)
            && EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                animationPlayer.Play(GetAnimation(jittering, sprite), _jitterAnimationKey);
        }

        private Animation GetAnimation(JitteringComponent jittering, SpriteComponent sprite)
        {
            var amplitude = MathF.Min(4f, jittering.Amplitude / 100f + 1f) / 10f;
            var offset = new Vector2(_random.NextFloat(amplitude/4f, amplitude),
                _random.NextFloat(amplitude / 4f, amplitude / 3f));

            offset.X *= _random.Pick(_sign);
            offset.Y *= _random.Pick(_sign);

            if (Math.Sign(offset.X) == Math.Sign(jittering.LastJitter.X)
                || Math.Sign(offset.Y) == Math.Sign(jittering.LastJitter.Y))
            {
                // If the sign is the same as last time on both axis we flip one randomly
                // to avoid jitter staying in one quadrant too much.
                if (_random.Prob(0.5f))
                    offset.X *= -1;
                else
                    offset.Y *= -1;
            }

            // Animation length shouldn't be too high so we will cap it at 2 seconds...
            var length = Math.Min((1f/jittering.Frequency), 2f);

            jittering.LastJitter = offset;

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
                            new AnimationTrackProperty.KeyFrame(offset, length),
                        }
                    }
                }
            };
        }
    }
}

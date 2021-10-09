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
    public class JitteringSystem : SharedJitteringSystem
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
            if (!EntityManager.TryGetComponent(uid, out ISpriteComponent? sprite))
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
            if(args.Key != _jitterAnimationKey || jittering.EndTime <= GameTiming.CurTime)
                return;

            if(EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? animationPlayer)
            && EntityManager.TryGetComponent(uid, out ISpriteComponent? sprite))
                animationPlayer.Play(GetAnimation(jittering, sprite), _jitterAnimationKey);
        }

        private Animation GetAnimation(JitteringComponent jittering, ISpriteComponent sprite)
        {
            var amplitude = MathF.Min(4f, jittering.Amplitude / 100f + 1f) / 10f;
            var sign = _random.Pick(_sign);
            var offset = new Vector2(_random.NextFloat(amplitude/4f, amplitude) * sign,
                _random.NextFloat(amplitude / 4f, amplitude / 3f) * -sign);

            // Animation length shouldn't be too high so we will cap it at 2 seconds...
            var length = Math.Min((1f/jittering.Frequency), 2f);

            return new Animation()
            {
                Length = TimeSpan.FromSeconds(length),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(ISpriteComponent),
                        Property = nameof(ISpriteComponent.Offset),
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

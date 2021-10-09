using System;
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
            var animationPlayer = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);

            animationPlayer.Play(GetAnimation(jittering), _jitterAnimationKey);
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

            if(EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? animationPlayer))
                animationPlayer.Play(GetAnimation(jittering), _jitterAnimationKey);
        }

        private Animation GetAnimation(JitteringComponent jittering)
        {
            var amplitude = MathF.Min(4f, jittering.Amplitude / 100f + 1f) / 10f;
            var offset = new Vector2(_random.NextFloat(-amplitude, amplitude),
                _random.NextFloat(-amplitude / 3f, amplitude / 3f));

            return new Animation()
            {
                // No matter what, the animation always lasts half a second so it refreshes often.
                Length = TimeSpan.FromSeconds(0.5f),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(ISpriteComponent),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        Property = nameof(ISpriteComponent.Offset),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),

                            new AnimationTrackProperty.KeyFrame(offset, 0.125f),

                            new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.25f),

                            new AnimationTrackProperty.KeyFrame(-offset, 0.375f),

                            new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.5f),
                        }
                    }
                }
            };
        }
    }
}

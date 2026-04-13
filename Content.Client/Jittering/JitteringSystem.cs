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
        [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

        [Dependency] private readonly EntityQuery<AnimationPlayerComponent> _animationQuery = default!;
        [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

        private readonly string _jitterAnimationKey = "jittering";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<JitteringComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
            SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);

            // todo Yucky! This should be in the status effects relay but it's client only!
            SubscribeLocalEvent<StatusEffectContainerComponent, AnimationCompletedEvent>(MakeRelay);
            SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectRelayedEvent<AnimationCompletedEvent>>(OnRelayAnimationCompleted);
        }

        private void MakeRelay(Entity<StatusEffectContainerComponent> ent, ref AnimationCompletedEvent args)
        {
            _statusEffects.RelayEvent(ent, args);
        }

        private void OnShutdown(Entity<JitteringComponent> ent, ref ComponentShutdown args)
        {
            if (_spriteQuery.TryComp(ent, out var sprite))
                _sprite.SetOffset((ent, sprite), ent.Comp.StartOffset);
        }

        // Start the animation
        private void OnStatusApplied(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
        {
            ApplyJitter(args.Target, ent.Comp.Settings);
        }

        // End the animation if we're the last jitter
        private void OnStatusRemoved(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
        {
            if (_statusEffects.HasEffectComp<JitteringStatusEffectComponent>(args.Target))
                return;

            if (_animationQuery.TryComp(ent, out var animationPlayer))
                _animationPlayer.Stop(ent, animationPlayer, _jitterAnimationKey);

            RemCompDeferred<JitteringComponent>(args.Target);
        }

        // Repeat the animation
        private void OnRelayAnimationCompleted(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AnimationCompletedEvent> args)
        {
            if (args.Args.Key != _jitterAnimationKey)
                return;

            ApplyJitter(args.Args.Uid, ent.Comp.Settings);
        }

        private void ApplyJitter(EntityUid target, JitterSetting jitter)
        {
            var playerComp = EnsureComp<AnimationPlayerComponent>(target);
            if (_animationPlayer.HasRunningAnimation(target, _jitterAnimationKey))
                return; // If we're already playing a jitter, wait for next time

            if (!_spriteQuery.TryComp(target, out var spriteComp))
                return;

            // Save the starting offset to reset it later
            if (!EnsureComp<JitteringComponent>(target, out var jitterComp))
                jitterComp.StartOffset = spriteComp.Offset;

            // Create a random offset
            var offset = _random.NextVector2(jitter.MinRadius, jitter.MaxRadius);

            // If we're in the same quadrant as our current location, invert the offset
            // Reduces repetitive behavior and increases large movements
            if (Math.Sign(offset.X) == Math.Sign(spriteComp.Offset.X)
                && Math.Sign(offset.Y) == Math.Sign(spriteComp.Offset.Y))
            {
                offset = -offset;
            }

            offset = Vector2.Transform(offset, jitter.Matrix);

            // avoid dividing by 0 so animations don't try to be infinitely long
            var length = jitter.Frequency <= 0 ? 0f : 1f / jitter.Frequency;

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
                            new AnimationTrackProperty.KeyFrame(spriteComp.Offset, 0f),
                            new AnimationTrackProperty.KeyFrame(jitterComp.StartOffset + offset, length),
                        },
                    },
                },
            };
            _animationPlayer.Play((target, playerComp), animation, _jitterAnimationKey);
        }
    }
}

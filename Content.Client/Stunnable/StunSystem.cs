using System.Numerics;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        [Dependency] private readonly AnimationPlayerSystem _animation = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private readonly int[] _sign = [-1, 1];
        public const string StunnedAnimationKeyVector = "stunnedVector";
        public const string StunnedAnimationKeyRotation = "stunnedAngle";
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunnedComponent, StunAnimationEvent>(OnStunAnimation);
            SubscribeLocalEvent<StunnedComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        }

        private void OnStunAnimation(Entity<StunnedComponent> ent, ref StunAnimationEvent args)
        {
            if (!TryComp<SpriteComponent>(ent, out var sprite) || !_timing.IsFirstTimePredicted)
                return;

            if (_animation.HasRunningAnimation(ent, StunnedAnimationKeyVector) || _animation.HasRunningAnimation(ent, StunnedAnimationKeyRotation))
                return;

            var newTime = _timing.CurTime + args.Time;
            ent.Comp.StartOffset = sprite.Offset;
            ent.Comp.StartAngle = sprite.Rotation;

            if (TimeSpan.Compare(newTime, ent.Comp.AnimationEnd) == 1)
                    ent.Comp.AnimationEnd = newTime;

            PlayStunnedAnimation(ent, sprite);
        }

        protected override void OnStunShutdown(Entity<StunnedComponent> ent, ref ComponentShutdown args)
        {
            base.OnStunShutdown(ent, ref args);

            if (!_timing.IsFirstTimePredicted)
                return;

            var ev = new StunAnimationEndEvent();
            RaiseLocalEvent(ent, ref ev);

            if (!TryComp<AnimationPlayerComponent>(ent, out var animation))
                return;

            // Standing system should handle the angle offset so we don't need to update that
            if (TryComp(ent, out SpriteComponent? sprite))
                sprite.Offset = ent.Comp.StartOffset;

            _animation.Stop(ent.Owner, animation, StunnedAnimationKeyVector);
            _animation.Stop(ent.Owner, animation, StunnedAnimationKeyRotation);
        }

        private void OnAnimationCompleted(Entity<StunnedComponent> ent, ref AnimationCompletedEvent args)
        {
            if (args.Key != StunnedAnimationKeyVector && args.Key != StunnedAnimationKeyRotation || !args.Finished)
                return;

            if (_timing.CurTime < ent.Comp.AnimationEnd)
            {
                var ev = new StunAnimationEndEvent();
                RaiseLocalEvent(ent, ref ev);
                return;
            }

            if (!HasComp<AnimationPlayerComponent>(ent)
                || !TryComp<SpriteComponent>(ent, out var sprite))
                return;

            switch (args.Key)
            {
                case StunnedAnimationKeyVector:
                    _animation.Play(ent.Owner,
                        GetFatigueAnimation(sprite,
                            ent.Comp.Frequency,
                            ent.Comp.Jitters,
                            (ent.Comp.Amplitude/2, ent.Comp.Amplitude),
                            (ent.Comp.Amplitude/8, ent.Comp.Amplitude/4),
                            ent.Comp.BreathingAmplitude,
                            ent.Comp.StartOffset,
                            ref ent.Comp.LastJitter),
                        StunnedAnimationKeyVector);
                    break;
                case StunnedAnimationKeyRotation:
                    _animation.Play(ent.Owner,
                        GetTwitchAnimation(sprite,
                            ent.Comp.RotationFrequency,
                            (ent.Comp.Torque/4, ent.Comp.Torque),
                            ent.Comp.StartAngle),
                        StunnedAnimationKeyRotation);
                    break;
            }
        }

        private void PlayStunnedAnimation(Entity<StunnedComponent> ent, SpriteComponent sprite)
        {
            _animation.Play(ent.Owner,
                GetFatigueAnimation(sprite,
                    ent.Comp.Frequency,
                    ent.Comp.Jitters,
                    (ent.Comp.Amplitude/2, ent.Comp.Amplitude),
                    (ent.Comp.Amplitude/8, ent.Comp.Amplitude/4),
                    ent.Comp.BreathingAmplitude,
                    ent.Comp.StartOffset,
                    ref ent.Comp.LastJitter),
                StunnedAnimationKeyVector);

            _animation.Play(ent.Owner,
                GetTwitchAnimation(sprite,
                ent.Comp.RotationFrequency,
                (ent.Comp.Torque/4, ent.Comp.Torque),
                ent.Comp.StartAngle),
                StunnedAnimationKeyRotation);
        }

        public Animation GetTwitchAnimation(SpriteComponent sprite, float frequency, (float, float) rotateClamp, Angle startAngle)
        {
            // avoid animations with negative length or infinite length
            if (frequency <= 0)
                return new Animation();

            // This is only half the animation, the other hal
            var rotation = new Angle(_random.NextFloat(rotateClamp.Item1, rotateClamp.Item2));

            rotation *= _random.Pick(_sign);

            var length = 1f / frequency;

            return new Animation
            {
                Length = TimeSpan.FromSeconds(length),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Rotation),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0f),
                            new AnimationTrackProperty.KeyFrame(startAngle + rotation, length/2),
                            new AnimationTrackProperty.KeyFrame(startAngle + rotation, length), // hold the angle
                        }
                    }
                }
            };
        }

        public Animation GetFatigueAnimation(SpriteComponent sprite, float frequency, int jitters, (float, float) jitterX, (float, float) jitterY, float breathing, Vector2 startOffset, ref Vector2 lastJitter)
        {
            // avoid animations with negative length or infinite length
            if (frequency <= 0)
                return new Animation();

            var offsets = new List<Vector2>();

            for (var i = 0; i < jitters; i++)
            {
                offsets.Add(new Vector2(_random.NextFloat(jitterX.Item1, jitterX.Item2),
                    _random.NextFloat(jitterY.Item1, jitterY.Item2)));

                var offset = offsets[i];
                offset.X *= _random.Pick(_sign);
                offset.Y *= _random.Pick(_sign);

                if (i < 1 && Math.Sign(offset.X) == Math.Sign(lastJitter.X)
                           && Math.Sign(offset.Y) == Math.Sign(lastJitter.Y))
                {
                    // If the sign is the same as last time on both axis we flip one randomly
                    // to avoid jitter staying in one quadrant too much.
                    if (_random.Prob(0.5f))
                        offset.X *= -1;
                    else
                        offset.Y *= -1;
                }

                lastJitter = offset;
                offsets[i] = offset;
            }

            var breaths = new Vector2(0, breathing) / jitters;

            var length =  1 / frequency;
            var frames = length / jitters;

            var keyFrames = new List<AnimationTrackProperty.KeyFrame>();
            keyFrames.Add(new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f));

            for (var i = 1; i <= jitters; i++)
            {
                if (i <= jitters / 2)
                {
                    keyFrames.Add(new AnimationTrackProperty.KeyFrame(startOffset + breaths * i + offsets[i - 1], frames * i));
                }
                else if (i < jitters * 3 / 4)
                {
                    keyFrames.Add(
                        new AnimationTrackProperty.KeyFrame(startOffset + breaths * ( jitters - i * 1.5f ) + offsets[i - 1], frames * i));
                }
                else
                {
                    keyFrames.Add(
                        new AnimationTrackProperty.KeyFrame(startOffset + breaths * ( i - jitters ) + offsets[i - 1], frames * i));
                }
            }

            return new Animation
            {
                Length = TimeSpan.FromSeconds(length),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        // Heavy Breathing
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Offset),
                        InterpolationMode = AnimationInterpolationMode.Cubic,
                        KeyFrames = keyFrames,
                    },
                }
            };
        }

        /// <summary>
        ///     Raised on an entity when its stun animation ends.
        ///     This is because animations bulldoze each other so they need to know who is next in line
        /// </summary>
        [ByRefEvent]
        public record struct StunAnimationEndEvent;
    }
}

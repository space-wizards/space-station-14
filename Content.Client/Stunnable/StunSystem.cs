using System.Numerics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        [Dependency] private readonly AnimationPlayerSystem _animation = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SpriteSystem _spriteSystem = default!;

        private readonly int[] _sign = [-1, 1];
        public const string StunnedAnimationKeyVector = "stunnedVector";
        public const string StunnedAnimationKeyRotation = "stunnedAngle";
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunnedComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<StunnedComponent, AnimationCompletedEvent>(OnAnimationCompleted);
            SubscribeLocalEvent<StunnedComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<StunnedComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        }

        /// <summary>
        ///     Add stun visual layers
        /// </summary>
        private void OnComponentInit(Entity<StunnedComponent> entity, ref ComponentInit args)
        {
            if (_timing.IsFirstTimePredicted)
                return;

            if (!TryComp<SpriteComponent>(entity, out var sprite))
                return;

            _spriteSystem.LayerMapReserve((entity, sprite), StunVisualLayers.StamCrit);
            _spriteSystem.LayerSetVisible((entity, sprite), StunVisualLayers.StamCrit, entity.Comp.Visualized);
            _spriteSystem.LayerSetOffset((entity, sprite), StunVisualLayers.StamCrit, new Vector2(0, 0.3125f));

            _spriteSystem.LayerSetRsi((entity.Owner, sprite), StunVisualLayers.StamCrit, new ResPath("Mobs/Effects/stunned.rsi"));

            UpdateAppearance((entity, sprite, entity.Comp));
        }

        private void OnAppearanceChanged(Entity<StunnedComponent> entity, ref AppearanceChangeEvent args)
        {
            if (args.Sprite != null)
                UpdateAppearance((entity, args.Sprite, entity.Comp));
        }

        private void UpdateAppearance(Entity<SpriteComponent?, StunnedComponent> entity)
        {
            if ( !Resolve(entity, ref entity.Comp1))
                return;

            if (!_spriteSystem.LayerMapTryGet(entity, StunVisualLayers.StamCrit, out var index, false))
                return;

            _spriteSystem.LayerSetVisible(entity, index, entity.Comp2.Visualized);
            _spriteSystem.LayerSetRsiState(entity, index, "stunned");
        }

        /// <summary>
        ///     Trys to play a default stun animation
        /// </summary>
        public override void TryStunAnimation(Entity<StunnedComponent> ent, TimeSpan time)
        {
            base.TryStunAnimation(ent, time);

            if (!TryComp<SpriteComponent>(ent, out var sprite) || !_timing.IsFirstTimePredicted)
                return;

            UpdateAppearance((ent, sprite, ent.Comp));

            if (_animation.HasRunningAnimation(ent, StunnedAnimationKeyVector) || _animation.HasRunningAnimation(ent, StunnedAnimationKeyRotation))
                return;

            // Don't animate if we're dead
            if (TryComp<MobStateComponent>(ent, out var state) && state.CurrentState == MobState.Dead)
                return;

            var newTime = _timing.CurTime + time;
            ent.Comp.StartOffset = sprite.Offset;
            ent.Comp.StartAngle = sprite.Rotation;

            if (TimeSpan.Compare(newTime, ent.Comp.AnimationEnd) == 1)
                    ent.Comp.AnimationEnd = newTime;

            var ev = new StunAnimationEvent(time);
            RaiseLocalEvent(ent, ref ev);

            PlayStunnedAnimation(ent, sprite);
        }

        protected override void OnStunShutdown(Entity<StunnedComponent> ent, ref ComponentShutdown args)
        {
            base.OnStunShutdown(ent, ref args);

            if (!_timing.IsFirstTimePredicted)
                return;

            // Standing system should handle the angle offset so we don't need to update that
            if (TryComp(ent, out SpriteComponent? sprite))
                _spriteSystem.SetOffset((ent, sprite), ent.Comp.StartOffset);

            if (!HasComp<AnimationPlayerComponent>(ent))
                return;

            StopStunnedAnimation(ent);
        }

        private void OnMobStateChanged(Entity<StunnedComponent> ent, ref MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
                StopStunnedAnimation(ent);
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

        private void StopStunnedAnimation(Entity<StunnedComponent> ent)
        {
            var ev = new StunAnimationEndEvent();
            RaiseLocalEvent(ent, ref ev);

            _animation.Stop(ent.Owner, StunnedAnimationKeyVector);
            _animation.Stop(ent.Owner, StunnedAnimationKeyRotation);
        }

        public Animation GetTwitchAnimation(SpriteComponent sprite,
            float frequency,
            (float, float) rotateClamp,
            Angle startAngle)
        {
            // avoid animations with negative length or infinite length
            if (frequency <= 0)
                return new Animation();

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
                            // hold the rotation for 50% of the time
                        }
                    }
                }
            };
        }

        public Animation GetFatigueAnimation(SpriteComponent sprite,
            float frequency,
            int jitters,
            (float, float) jitterX,
            (float, float) jitterY,
            float breathing,
            Vector2 startOffset,
            ref Vector2 lastJitter)
        {
            // avoid animations with negative length or infinite length
            if (frequency <= 0)
                return new Animation();

            var breaths = new Vector2(0, breathing) / jitters;

            var length =  1 / frequency;
            var frames = length / jitters;

            var keyFrames = new List<AnimationTrackProperty.KeyFrame> { new(sprite.Offset, 0f) };

            for (var i = 1; i <= jitters; i++)
            {
                var offset = new Vector2(_random.NextFloat(jitterX.Item1, jitterX.Item2),
                    _random.NextFloat(jitterY.Item1, jitterY.Item2));
                offset.X *= _random.Pick(_sign);
                offset.Y *= _random.Pick(_sign);

                if (i == 1 && Math.Sign(offset.X) == Math.Sign(lastJitter.X)
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

                if (i <= jitters / 2)
                {
                    keyFrames.Add(new AnimationTrackProperty.KeyFrame(startOffset + breaths * i + offset, frames));
                }
                else if (i < jitters * 3 / 4)
                {
                    keyFrames.Add(
                        new AnimationTrackProperty.KeyFrame(startOffset + breaths * ( jitters - i * 1.5f ) + offset, frames));
                }
                else
                {
                    keyFrames.Add(
                        new AnimationTrackProperty.KeyFrame(startOffset + breaths * ( i - jitters ) + offset, frames));
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

        /// <summary>
        ///     Raised when you want a stunned entity to play its stun animation for a certain amount of time.
        /// </summary>
        [ByRefEvent]
        public record struct StunAnimationEvent(TimeSpan Time);
    }

    public enum StunVisualLayers : byte
    {
        StamCrit
    }
}

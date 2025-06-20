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
            _spriteSystem.LayerSetVisible((entity, sprite), StunVisualLayers.StamCrit, false);
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
            if (!Resolve(entity, ref entity.Comp1))
                return;

            if (!_spriteSystem.LayerMapTryGet(entity, StunVisualLayers.StamCrit, out var index, false))
                return;

            // Don't animate if we're not conscious
            if (!Blocker.CanConsciouslyPerformAction(entity))
                entity.Comp2.Visualized = false;

            _spriteSystem.LayerSetVisible(entity, index, entity.Comp2.Visualized);
            _spriteSystem.LayerSetRsiState(entity, index, "stunned");
        }

        private void OnMobStateChanged(Entity<StunnedComponent> entity, ref MobStateChangedEvent args)
        {
            if (!Blocker.CanConsciouslyPerformAction(entity))
                entity.Comp.Visualized = false;

            if (!TryComp<SpriteComponent>(entity, out var sprite))
                return;

            if (!_spriteSystem.LayerMapTryGet((entity, sprite), StunVisualLayers.StamCrit, out var index, false))
                return;

            _spriteSystem.LayerSetVisible((entity, sprite), index, entity.Comp.Visualized);
        }

        /// <summary>
        /// A simple random rotation animation
        /// </summary>
        /// <param name="sprite">The spriteComp we're rotating</param>
        /// <param name="frequency">How many times per second we're rotating</param>
        /// <param name="rotateClamp">Maximum angle of rotation (in radians)</param>
        /// <param name="startAngle">Default starting angle of rotation (used because we don't have adjustment layers)</param>
        /// <returns></returns>
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

        /// <summary>
        /// A simple fatigue animation, a mild modification of the jittering animation
        /// </summary>
        /// <param name="sprite">The spriteComponent we're adjusting the offset of</param>
        /// <param name="frequency">How many times per second does the animation run?</param>
        /// <param name="jitters">How many times should we jitter during the animation?</param>
        /// <param name="jitterX">Maximum jitter offset in the X direction</param>
        /// <param name="jitterY">Maximum jitter offset in the Y direction</param>
        /// <param name="breathing">Maximum breathing offset, this is in the Y direction</param>
        /// <param name="startOffset">Starting offset because we don't have adjustment layers</param>
        /// <param name="lastJitter">Last jitter so we don't jitter to the same quadrant</param>
        /// <returns></returns>
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

            var breaths = new Vector2(0, breathing * 2) / jitters;

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

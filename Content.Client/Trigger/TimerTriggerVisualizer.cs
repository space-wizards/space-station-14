using System;
using Content.Shared.Sound;
using Content.Shared.Trigger;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Trigger
{
    [UsedImplicitly]
    public class TimerTriggerVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "priming_animation";

        [DataField("countdown_sound", required: true)]
        private SoundSpecifier _countdownSound = default!;

        private Animation PrimingAnimation = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            PrimingAnimation = new Animation { Length = TimeSpan.MaxValue };
            {
                var flick = new AnimationTrackSpriteFlick();
                PrimingAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = TriggerVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("primed", 0f));

                var sound = new AnimationTrackPlaySound();
                PrimingAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(_countdownSound.GetSound(), 0));
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            if (!entity.HasComponent<AnimationPlayerComponent>())
            {
                IoCManager.Resolve<IEntityManager>().AddComponent<AnimationPlayerComponent>(entity);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner.Uid);
            var animPlayer = IoCManager.Resolve<IEntityManager>().GetComponent<AnimationPlayerComponent>(component.Owner.Uid);
            if (!component.TryGetData(TriggerVisuals.VisualState, out TriggerVisualState state))
            {
                state = TriggerVisualState.Unprimed;
            }

            switch (state)
            {
                case TriggerVisualState.Primed:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(PrimingAnimation, AnimationKey);
                    }
                    break;
                case TriggerVisualState.Unprimed:
                    sprite.LayerSetState(0, "icon");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    public enum TriggerVisualLayers : byte
    {
        Base
    }
}

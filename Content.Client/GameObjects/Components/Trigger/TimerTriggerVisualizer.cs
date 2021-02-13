using System;
using Content.Shared.GameObjects.Components.Trigger;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Trigger
{
    [UsedImplicitly]
    public class TimerTriggerVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "priming_animation";

        private Animation PrimingAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var countdownSound = node.GetNode("countdown_sound").AsString();


            PrimingAnimation = new Animation { Length = TimeSpan.MaxValue };
            {
                var flick = new AnimationTrackSpriteFlick();
                PrimingAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = TriggerVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("primed", 0f));

                var sound = new AnimationTrackPlaySound();
                PrimingAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(countdownSound, 0));
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            if (!entity.HasComponent<AnimationPlayerComponent>())
            {
                entity.AddComponent<AnimationPlayerComponent>();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
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

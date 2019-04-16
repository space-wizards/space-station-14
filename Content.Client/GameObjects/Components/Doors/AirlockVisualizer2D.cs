using System;
using Content.Shared.GameObjects.Components.Doors;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Doors
{
    public class AirlockVisualizer2D : AppearanceVisualizer
    {
        private const string AnimationKey = "airlock_animation";

        private Animation CloseAnimation;
        private Animation OpenAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var openSound = node.GetNode("open_sound").AsString();
            var closeSound = node.GetNode("close_sound").AsString();

            CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

                var sound = new AnimationTrackPlaySound();
                CloseAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));
            }

            OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

                var sound = new AnimationTrackPlaySound();
                OpenAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(openSound, 0));
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
            if (!component.TryGetData(DoorVisuals.VisualState, out DoorVisualState state))
            {
                state = DoorVisualState.Closed;
            }

            switch (state)
            {
                case DoorVisualState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    break;
                case DoorVisualState.Closing:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(CloseAnimation, AnimationKey);
                    }
                    break;
                case DoorVisualState.Opening:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(OpenAnimation, AnimationKey);
                    }

                    break;
                case DoorVisualState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum DoorVisualLayers
    {
        Base
    }
}

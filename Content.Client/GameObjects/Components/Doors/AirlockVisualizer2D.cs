using System;
using Content.Shared.GameObjects.Components.Doors;
using SS14.Client.Animations;
using SS14.Client.GameObjects;
using SS14.Client.GameObjects.Components.Animations;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Doors
{
    public class AirlockVisualizer2D : AppearanceVisualizer
    {
        private const string AnimationKey = "airlock_animation";

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

            // TODO: need some sorta state to prevent resetting the animation if it's already playing.
            // Because right now that could happen.
            animPlayer.Stop(AnimationKey);
            switch (state)
            {
                case DoorVisualState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    break;
                case DoorVisualState.Closing:
                    animPlayer.Play(CloseAnimation, AnimationKey);
                    break;
                case DoorVisualState.Opening:
                    animPlayer.Play(OpenAnimation, AnimationKey);
                    break;
                case DoorVisualState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static readonly Animation CloseAnimation;
        private static readonly Animation OpenAnimation;

        static AirlockVisualizer2D()
        {
            CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));
            }

            OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));
            }
        }
    }

    public enum DoorVisualLayers
    {
        Base
    }
}

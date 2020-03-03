using System;
using Content.Shared.GameObjects.Components.Power;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Power
{
    public class LatheVisualizer2D : AppearanceVisualizer
    {
        private const string AnimationKey = "lathe_animation";

        private Animation BuildingAnimation;
        private Animation InsertingAnimation;
        private Animation ReloadingAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            BuildingAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                BuildingAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = LatheVisualLayers.Building;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_building", 0.5f));
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
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out LatheVisualState state))
            {
                state = LatheVisualState.Base;
            }

            switch (state)
            {
                case LatheVisualState.Base:
                    sprite.LayerSetVisible(LatheVisualLayers.Building, false);
                    sprite.LayerSetVisible(LatheVisualLayers.Inserting, false);
                    sprite.LayerSetVisible(LatheVisualLayers.Reloading, false);
                    break;
                case LatheVisualState.Building:
                case LatheVisualState.Inserting:
                case LatheVisualState.Reloading:
                    // if (!animPlayer.HasRunningAnimation(AnimationKey))
                    // {
                    //     //animPlayer.Play();
                    // }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum LatheVisualLayers
    {
        Building,
        Inserting,
        Reloading
    }
}

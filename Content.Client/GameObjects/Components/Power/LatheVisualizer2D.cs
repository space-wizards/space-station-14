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
        private Animation InsertingMetalAnimation;
        private Animation InsertingGlassAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            BuildingAnimation = new Animation {Length = TimeSpan.FromSeconds(1.35f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                BuildingAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = LatheVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_building", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                BuildingAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = LatheVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_building_unlit", 0f));
            }

            InsertingMetalAnimation = new Animation {Length = TimeSpan.FromSeconds(0.9f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                InsertingMetalAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = LatheVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_inserting_metal_plate", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                InsertingMetalAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = LatheVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_inserting_unlit", 0f));
            }

            InsertingGlassAnimation = new Animation {Length = TimeSpan.FromSeconds(0.9f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                InsertingGlassAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = LatheVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_inserting_glass_plate", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                InsertingGlassAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = LatheVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("autolathe_inserting_unlit", 0f));
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
                    sprite.LayerSetState(LatheVisualLayers.Base, "autolathe");
                    sprite.LayerSetState(LatheVisualLayers.BaseUnlit, "autolathe_unlit");
                    if (animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Stop(AnimationKey);
                    }
                    break;
                case LatheVisualState.Producing:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(BuildingAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingMetal:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(InsertingMetalAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingGlass:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(InsertingGlassAnimation, AnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum LatheVisualLayers
    {
        Base,
        BaseUnlit
    }
}

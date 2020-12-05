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
    public class AutolatheVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "autolathe_animation";

        private Animation _buildingAnimation;
        private Animation _insertingMetalAnimation;
        private Animation _insertingGlassAnimation;
        private Animation _insertingGoldAnimation;
        private Animation _insertingPhoronAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _buildingAnimation = PopulateAnimation("autolathe_building", "autolathe_building_unlit", 0.5f);
            _insertingMetalAnimation = PopulateAnimation("autolathe_inserting_metal_plate", "autolathe_inserting_unlit", 0.9f);
            _insertingGlassAnimation = PopulateAnimation("autolathe_inserting_glass_plate", "autolathe_inserting_unlit", 0.9f);
            _insertingGoldAnimation = PopulateAnimation("autolathe_inserting_gold_plate", "autolathe_inserting_unlit", 0.9f);
            _insertingPhoronAnimation = PopulateAnimation("autolathe_inserting_phoron_sheet", "autolathe_inserting_unlit", 0.9f);
        }

        private Animation PopulateAnimation(string sprite, string spriteUnlit, float length)
        {
            var animation = new Animation {Length = TimeSpan.FromSeconds(length)};

            var flick = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flick);
            flick.LayerKey = AutolatheVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(sprite, 0f));

            var flickUnlit = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flickUnlit);
            flickUnlit.LayerKey = AutolatheVisualLayers.BaseUnlit;
            flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(spriteUnlit, 0f));

            return animation;
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
                state = LatheVisualState.Idle;
            }

            switch (state)
            {
                case LatheVisualState.Idle:
                    if (animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Stop(AnimationKey);
                    }

                    sprite.LayerSetState(AutolatheVisualLayers.Base, "autolathe");
                    sprite.LayerSetState(AutolatheVisualLayers.BaseUnlit, "autolathe_unlit");
                    break;
                case LatheVisualState.Producing:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_buildingAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingMetal:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingMetalAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingGlass:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingGlassAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingGold:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingGoldAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingPhoron:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingPhoronAnimation, AnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(AutolatheVisualLayers.BaseUnlit, glowingPartsVisible);
        }
        public enum AutolatheVisualLayers : byte
        {
            Base,
            BaseUnlit
        }
    }
}

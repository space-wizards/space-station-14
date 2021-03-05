using System;
using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Power
{
    [UsedImplicitly]
    public class ProtolatheVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "inserting_animation";

        private Animation _buildingAnimation;
        private Animation _insertingMetalAnimation;
        private Animation _insertingGlassAnimation;
        private Animation _insertingGoldAnimation;
        private Animation _insertingPlasmaAnimation;
        private Animation _insertingPlasticAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _buildingAnimation = PopulateAnimation("building", "building_unlit", 0.8f);
            _insertingMetalAnimation = PopulateAnimation("inserting_metal", "inserting_unlit", 0.8f);
            _insertingGlassAnimation = PopulateAnimation("inserting_glass", "inserting_unlit", 0.8f);
            _insertingGoldAnimation = PopulateAnimation("inserting_gold", "inserting_unlit", 0.8f);
            _insertingPlasmaAnimation = PopulateAnimation("inserting_plasma", "inserting_unlit", 0.8f);
            _insertingPlasticAnimation = PopulateAnimation("inserting_plastic", "inserting_unlit", 0.8f);
        }

        private Animation PopulateAnimation(string sprite, string spriteUnlit, float length)
        {
            var animation = new Animation { Length = TimeSpan.FromSeconds(length) };

            var flick = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flick);
            flick.LayerKey = ProtolatheVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(sprite, 0f));

            var flickUnlit = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flickUnlit);
            flickUnlit.LayerKey = ProtolatheVisualLayers.BaseUnlit;
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
            sprite.LayerSetVisible(ProtolatheVisualLayers.AnimationLayer, true);
            switch (state)
            {
                case LatheVisualState.Idle:
                    if (animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Stop(AnimationKey);
                    }

                    sprite.LayerSetState(ProtolatheVisualLayers.Base, "icon");
                    sprite.LayerSetState(ProtolatheVisualLayers.BaseUnlit, "unlit");
                    sprite.LayerSetVisible(ProtolatheVisualLayers.AnimationLayer, false);
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
                case LatheVisualState.InsertingPlasma:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingPlasmaAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingPlastic:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingPlasticAnimation, AnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(ProtolatheVisualLayers.BaseUnlit, glowingPartsVisible);
        }
        public enum ProtolatheVisualLayers : byte
        {
            Base,
            BaseUnlit,
            AnimationLayer
        }
    }
}

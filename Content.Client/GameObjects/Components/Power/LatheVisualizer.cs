using System;
using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Power
{
    [UsedImplicitly]
    public class LatheVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string InsertingAnimationKey = "inserting_animation";
        private const string ProducingAnimationKey = "producing_animation";

        [DataField("stateInserting", required: true)]
        private string? stateInserting = "inserting";

        [DataField("stateProducing", required: true)]
        private string? stateProducing = "producing";

        [DataField("stateInsertingUnlit", required: true)]
        private string? stateInsertingUnlit = "inserting_unlit";

        [DataField("stateProducingUnlit", required: true)]
        private string? stateProducingUnlit = "producing_unlit";

        [DataField("productionAnimationTime", required: true)]
        private float producingAnimationTime = 0.9f;

        [DataField("insertingAnimationTime", required: true)]
        private float insertingAnimationTime = 0.9f;

        private Animation _producingAnimation = default!;
        private Animation _insertingAnimation = default!;
        private AnimationPlayerComponent _animPlayer = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            _producingAnimation = new Animation {Length = TimeSpan.FromSeconds(producingAnimationTime)};
            var pFlick = new AnimationTrackSpriteFlick();
            var pUnlitFlick = new AnimationTrackSpriteFlick();

            _producingAnimation.AnimationTracks.Add(pFlick);
            _producingAnimation.AnimationTracks.Add(pUnlitFlick);
            pFlick.LayerKey = LatheVisualLayers.Producing;
            pUnlitFlick.LayerKey = LatheVisualLayers.Unlit;
            pFlick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(stateProducing, 0f));
            pUnlitFlick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(stateProducingUnlit, 0f));

            _insertingAnimation = new Animation {Length = TimeSpan.FromSeconds(insertingAnimationTime)};
            var iFlick = new AnimationTrackSpriteFlick();
            var iUnlitFlick = new AnimationTrackSpriteFlick();

            _insertingAnimation.AnimationTracks.Add(iFlick);
            _insertingAnimation.AnimationTracks.Add(iUnlitFlick);
            iFlick.LayerKey = LatheVisualLayers.Inserting;
            iUnlitFlick.LayerKey = LatheVisualLayers.Unlit;
            iFlick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(stateInserting, 0f));
            iUnlitFlick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(stateInsertingUnlit, 0f));
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            _animPlayer = entity.EnsureComponent<AnimationPlayerComponent>();
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(LatheVisualData.State, out LatheVisualState state))
            {
                state = LatheVisualState.Idle;
            }
            if (!component.TryGetData(LatheVisualData.Color, out Color color))
            {
                color = Color.FromHex("#ffffff");
            }
            switch (state)
            {
                case LatheVisualState.Idle:
                    sprite.LayerSetState(LatheVisualLayers.Base, "icon");
                    sprite.LayerSetState(LatheVisualLayers.Unlit, "unlit");
                    //sprite.LayerSetVisible(LatheVisualLayers.Inserting, false);
                    //sprite.LayerSetVisible(LatheVisualLayers.Producing, false);
                    break;
                case LatheVisualState.Producing:
                    //sprite.LayerSetVisible(LatheVisualLayers.Producing, true);
                    if (!_animPlayer.HasRunningAnimation(ProducingAnimationKey))
                    {
                        _animPlayer.Play(_producingAnimation, ProducingAnimationKey);
                    }
                    break;
                case LatheVisualState.Inserting:
                    //sprite.LayerSetVisible(LatheVisualLayers.Inserting, true);
                    sprite.LayerSetColor(LatheVisualLayers.Inserting, color);
                    if (!_animPlayer.HasRunningAnimation(InsertingAnimationKey))
                    {
                        _animPlayer.Play(_insertingAnimation, InsertingAnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var glowingPartsVisible = component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && powered;
            sprite.LayerSetVisible(LatheVisualLayers.Unlit, glowingPartsVisible);
        }

        public enum LatheVisualLayers : byte
        {
            Base,
            Unlit,
            Inserting,
            Producing
        }
    }
}

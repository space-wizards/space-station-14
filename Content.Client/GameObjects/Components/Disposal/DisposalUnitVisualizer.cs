using System;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalUnitComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class DisposalUnitVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "disposal_unit_animation";

        private string _stateAnchored;
        private string _stateUnAnchored;
        private string _stateCharging;
        private string _overlayCharging;
        private string _overlayReady;
        private string _overlayFull;
        private string _overlayEngaged;
        private string _stateFlush;

        private Animation _flushAnimation;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.TryGetData(Visuals.VisualState, out VisualState state))
            {
                return;
            }

            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            switch (state)
            {
                case VisualState.UnAnchored:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, _stateUnAnchored);
                    break;
                case VisualState.Anchored:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, _stateAnchored);
                    break;
                case VisualState.Charging:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, _stateCharging);
                    break;
                case VisualState.Flushing:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, _stateAnchored);

                    var animPlayer = appearance.Owner.GetComponent<AnimationPlayerComponent>();

                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_flushAnimation, AnimationKey);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!appearance.TryGetData(Visuals.Handle, out HandleState handleState))
            {
                handleState = HandleState.Normal;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.Handle, handleState != HandleState.Normal);

            switch (handleState)
            {
                case HandleState.Normal:
                    break;
                case HandleState.Engaged:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Handle, _overlayEngaged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!appearance.TryGetData(Visuals.Light, out LightState lightState))
            {
                lightState = LightState.Off;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.Light, lightState != LightState.Off);

            switch (lightState)
            {
                case LightState.Off:
                    break;
                case LightState.Charging:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Light, _overlayCharging);
                    break;
                case LightState.Full:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Light, _overlayFull);
                    break;
                case LightState.Ready:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Light, _overlayReady);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _stateAnchored = node.GetNode("state_anchored").AsString();
            _stateUnAnchored = node.GetNode("state_unanchored").AsString();
            _stateCharging = node.GetNode("state_charging").AsString();
            _overlayCharging = node.GetNode("overlay_charging").AsString();
            _overlayReady = node.GetNode("overlay_ready").AsString();
            _overlayFull = node.GetNode("overlay_full").AsString();
            _overlayEngaged = node.GetNode("overlay_engaged").AsString();
            _stateFlush = node.GetNode("state_flush").AsString();

            var flushSound = node.GetNode("flush_sound").AsString();
            var flushTime = node.GetNode("flush_time").AsFloat();

            _flushAnimation = new Animation {Length = TimeSpan.FromSeconds(flushTime)};

            var flick = new AnimationTrackSpriteFlick();
            _flushAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DisposalUnitVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(_stateFlush, 0));

            var sound = new AnimationTrackPlaySound();
            _flushAnimation.AnimationTracks.Add(sound);
            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(flushSound, 0));
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            entity.EnsureComponent<AnimationPlayerComponent>();
            var appearance = entity.EnsureComponent<AppearanceComponent>();

            ChangeState(appearance);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            ChangeState(component);
        }
    }

    public enum DisposalUnitVisualLayers : byte
    {
        Base,
        Handle,
        Light
    }
}

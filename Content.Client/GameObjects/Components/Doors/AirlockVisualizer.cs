using System;
using Content.Client.GameObjects.Components.Wires;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Doors
{
    [UsedImplicitly]
    public class AirlockVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "airlock_animation";

        [DataField("open_sound", required: true)]
        private string _openSound = default!;

        [DataField("close_sound", required: true)]
        private string _closeSound = default!;

        [DataField("deny_sound", required: true)]
        private string _denySound = default!;

        [DataField("animation_time")]
        private float _delay = 0.8f;

        private Animation CloseAnimation = default!;
        private Animation OpenAnimation = default!;
        private Animation DenyAnimation = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing_unlit", 0f));

                var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flickMaintenancePanel);
                flickMaintenancePanel.LayerKey = WiresVisualizer.WiresVisualLayers.MaintenancePanel;
                flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_closing", 0f));

                var sound = new AnimationTrackPlaySound();
                CloseAnimation.AnimationTracks.Add(sound);

                if (_closeSound != null)
                {
                    sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(_closeSound, 0));
                }
            }

            OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening_unlit", 0f));

                var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flickMaintenancePanel);
                flickMaintenancePanel.LayerKey = WiresVisualizer.WiresVisualLayers.MaintenancePanel;
                flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_opening", 0f));

                var sound = new AnimationTrackPlaySound();
                OpenAnimation.AnimationTracks.Add(sound);

                if (_openSound != null)
                {
                    sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(_openSound, 0));
                }
            }

            DenyAnimation = new Animation {Length = TimeSpan.FromSeconds(0.3f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                DenyAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.BaseUnlit;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny_unlit", 0f));

                var sound = new AnimationTrackPlaySound();
                DenyAnimation.AnimationTracks.Add(sound);

                if (_denySound != null)
                {
                    sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(_denySound, 0, () => AudioHelpers.WithVariation(0.05f)));
                }
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
            if (!component.TryGetData(DoorVisuals.VisualState, out DoorVisualState state))
            {
                state = DoorVisualState.Closed;
            }

            var unlitVisible = true;
            var boltedVisible = false;
            var weldedVisible = false;

            if (animPlayer.HasRunningAnimation(AnimationKey))
            {
                animPlayer.Stop(AnimationKey);
            }
            switch (state)
            {
                case DoorVisualState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    unlitVisible = false;
                    break;
                case DoorVisualState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                    sprite.LayerSetState(DoorVisualLayers.BaseBolted, "bolted_unlit");
                    sprite.LayerSetState(WiresVisualizer.WiresVisualLayers.MaintenancePanel, "panel_open");
                    break;
                case DoorVisualState.Opening:
                    animPlayer.Play(OpenAnimation, AnimationKey);
                    break;
                case DoorVisualState.Closing:
                    animPlayer.Play(CloseAnimation, AnimationKey);
                    break;
                case DoorVisualState.Deny:
                    animPlayer.Play(DenyAnimation, AnimationKey);
                    break;
                case DoorVisualState.Welded:
                    weldedVisible = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (component.TryGetData(DoorVisuals.Powered, out bool powered) && !powered)
            {
                unlitVisible = false;
            }
            if (component.TryGetData(DoorVisuals.BoltLights, out bool lights) && lights)
            {
                boltedVisible = true;
            }

            sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
            sprite.LayerSetVisible(DoorVisualLayers.BaseWelded, weldedVisible);
            sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, unlitVisible && boltedVisible);
        }
    }

    public enum DoorVisualLayers : byte
    {
        Base,
        BaseUnlit,
        BaseWelded,
        BaseBolted,
    }
}

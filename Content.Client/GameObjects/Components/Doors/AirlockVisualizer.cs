using System;
using Content.Client.GameObjects.Components.Wires;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Doors
{
    [UsedImplicitly]
    public class AirlockVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "airlock_animation";

        private Animation CloseAnimation;
        private Animation OpenAnimation;
        private Animation DenyAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var delay = 0.8f;

            var openSound = node.GetNode("open_sound").AsString();
            var closeSound = node.GetNode("close_sound").AsString();
            var denySound = node.GetNode("deny_sound").AsString();
            if (node.TryGetNode("animation_time", out var yamlNode))
            {
                delay = yamlNode.AsFloat();
            }

            CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(delay)};
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
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));
            }

            OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(delay)};
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
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(openSound, 0));
            }

            DenyAnimation = new Animation {Length = TimeSpan.FromSeconds(0.3f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                DenyAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.BaseUnlit;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny", 0f));

                var sound = new AnimationTrackPlaySound();
                DenyAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(denySound, 0, () => AudioHelpers.WithVariation(0.05f)));
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
            if (component.Owner.Deleted)
                return;

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
            if (!component.TryGetData(DoorVisuals.VisualState, out DoorVisualState state))
            {
                state = DoorVisualState.Closed;
            }

            var unlitVisible = true;
            var boltedVisible = false;
            var weldedVisible = false;
            switch (state)
            {
                case DoorVisualState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                    sprite.LayerSetState(DoorVisualLayers.BaseBolted, "bolted");
                    sprite.LayerSetState(WiresVisualizer.WiresVisualLayers.MaintenancePanel, "panel_open");
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
                    unlitVisible = false;
                    break;
                case DoorVisualState.Deny:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(DenyAnimation, AnimationKey);
                    }
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

    public enum DoorVisualLayers
    {
        Base,
        BaseUnlit,
        BaseWelded,
        BaseBolted,
    }
}

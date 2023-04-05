using System;
using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Client.Doors
{
    [UsedImplicitly]
    public sealed class AirlockVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private const string AnimationKey = "airlock_animation";

        [DataField("animationTime")]
        private float _delay = 0.8f;

        [DataField("denyAnimationTime")]
        private float _denyDelay = 0.3f;


        [DataField("emagAnimationTime")]
        private float _delayEmag = 1.5f;

        /// <summary>
        ///     Whether the maintenance panel is animated or stays static.
        ///     False for windoors.
        /// </summary>
        [DataField("animatedPanel")]
        private bool _animatedPanel = true;

        /// <summary>
        /// Means the door is simply open / closed / opening / closing. No wires or access.
        /// </summary>
        [DataField("simpleVisuals")]
        private bool _simpleVisuals = false;

        /// <summary>
        ///     Whether the BaseUnlit layer should still be visible when the airlock
        ///     is opened.
        /// </summary>
        [DataField("openUnlitVisible")]
        private bool _openUnlitVisible = false;

        /// <summary>
        ///     Whether the door should have an emergency access layer
        /// </summary>
        [DataField("emergencyAccessLayer")]
        private bool _emergencyAccessLayer = true;

        private Animation CloseAnimation = default!;
        private Animation OpenAnimation = default!;
        private Animation DenyAnimation = default!;
        private Animation EmaggingAnimation = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);

            CloseAnimation = new Animation { Length = TimeSpan.FromSeconds(_delay) };
            {
                var flick = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

                if (!_simpleVisuals)
                {
                    var flickUnlit = new AnimationTrackSpriteFlick();
                    CloseAnimation.AnimationTracks.Add(flickUnlit);
                    flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                    flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing_unlit", 0f));

                    if (_animatedPanel)
                    {
                        var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                        CloseAnimation.AnimationTracks.Add(flickMaintenancePanel);
                        flickMaintenancePanel.LayerKey = WiresVisualLayers.MaintenancePanel;
                        flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_closing", 0f));
                    }
                }
            }

            OpenAnimation = new Animation { Length = TimeSpan.FromSeconds(_delay) };
            {
                var flick = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

                if (!_simpleVisuals)
                {
                    var flickUnlit = new AnimationTrackSpriteFlick();
                    OpenAnimation.AnimationTracks.Add(flickUnlit);
                    flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                    flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening_unlit", 0f));

                    if (_animatedPanel)
                    {
                        var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                        OpenAnimation.AnimationTracks.Add(flickMaintenancePanel);
                        flickMaintenancePanel.LayerKey = WiresVisualLayers.MaintenancePanel;
                        flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_opening", 0f));
                    }
                }
            }
            EmaggingAnimation = new Animation { Length = TimeSpan.FromSeconds(_delay) };
            {
                var flickUnlit = new AnimationTrackSpriteFlick();
                EmaggingAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("sparks", 0f));
            }

            if (!_simpleVisuals)
            {
                DenyAnimation = new Animation { Length = TimeSpan.FromSeconds(_denyDelay) };
                {
                    var flick = new AnimationTrackSpriteFlick();
                    DenyAnimation.AnimationTracks.Add(flick);
                    flick.LayerKey = DoorVisualLayers.BaseUnlit;
                    flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny_unlit", 0f));
                }
            }
        }

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            if (!_entMan.HasComponent<AnimationPlayerComponent>(entity))
            {
                _entMan.AddComponent<AnimationPlayerComponent>(entity);
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            // only start playing animations once.
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            base.OnChangeData(component);

            var sprite = _entMan.GetComponent<SpriteComponent>(component.Owner);
            var animPlayer = _entMan.GetComponent<AnimationPlayerComponent>(component.Owner);
            if (!component.TryGetData(DoorVisuals.State, out DoorState state))
            {
                state = DoorState.Closed;
            }

            var door = _entMan.GetComponent<DoorComponent>(component.Owner);

            if (component.TryGetData(DoorVisuals.BaseRSI, out string baseRsi))
            {
                if (!_resourceCache.TryGetResource<RSIResource>(SharedSpriteComponent.TextureRoot / baseRsi, out var res))
                {
                    Logger.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);
                }
                foreach (ISpriteLayer layer in sprite.AllLayers)
                {
                    layer.Rsi = res?.RSI;
                }
            }

            if (animPlayer.HasRunningAnimation(AnimationKey))
            {
                animPlayer.Stop(AnimationKey);
            }
            switch (state)
            {
                case DoorState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    if (_openUnlitVisible && !_simpleVisuals)
                    {
                        sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "open_unlit");
                    }
                    break;
                case DoorState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    if (!_simpleVisuals)
                    {
                        sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                        sprite.LayerSetState(DoorVisualLayers.BaseBolted, "bolted_unlit");
                    }
                    break;
                case DoorState.Opening:
                    animPlayer.Play(OpenAnimation, AnimationKey);
                    break;
                case DoorState.Closing:
                    if (door.CurrentlyCrushing.Count == 0)
                        animPlayer.Play(CloseAnimation, AnimationKey);
                    else
                        sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    break;
                case DoorState.Denying:
                    animPlayer.Play(DenyAnimation, AnimationKey);
                    break;
                case DoorState.Welded:
                    break;
                case DoorState.Emagging:
                    animPlayer.Play(EmaggingAnimation, AnimationKey);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_simpleVisuals)
                return;

            var boltedVisible = false;
            var emergencyLightsVisible = false;
            var unlitVisible = false;

            if (component.TryGetData(DoorVisuals.Powered, out bool powered) && powered)
            {
                boltedVisible = component.TryGetData(DoorVisuals.BoltLights, out bool lights) && lights;
                emergencyLightsVisible = component.TryGetData(DoorVisuals.EmergencyLights, out bool eaLights) && eaLights;
                unlitVisible = state == DoorState.Closing
                    || state == DoorState.Opening
                    || state == DoorState.Denying
                    || state == DoorState.Open && _openUnlitVisible
                    || (component.TryGetData(DoorVisuals.ClosedLights, out bool closedLights) && closedLights);
            }

            sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
            sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
            if (_emergencyAccessLayer)
            {
                sprite.LayerSetVisible(DoorVisualLayers.BaseEmergencyAccess,
                        emergencyLightsVisible
                        && state != DoorState.Open
                        && state != DoorState.Opening
                        && state != DoorState.Closing);
            }
        }
    }

    public enum DoorVisualLayers : byte
    {
        Base,
        BaseUnlit,
        BaseBolted,
        BaseEmergencyAccess,
    }
}

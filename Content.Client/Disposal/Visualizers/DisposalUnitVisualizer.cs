using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.Visualizers
{
    [UsedImplicitly]
    public sealed class DisposalUnitVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "disposal_unit_animation";

        [DataField("state_anchored", required: true)]
        private string? _stateAnchored;

        [DataField("state_unanchored", required: true)]
        private string? _stateUnAnchored;

        [DataField("state_charging", required: true)]
        private string? _stateCharging;

        [DataField("overlay_charging", required: true)]
        private string? _overlayCharging;

        [DataField("overlay_ready", required: true)]
        private string? _overlayReady;

        [DataField("overlay_full", required: true)]
        private string? _overlayFull;

        [DataField("overlay_engaged", required: true)]
        private string? _overlayEngaged;

        [DataField("state_flush", required: true)]
        private string? _stateFlush;

        [DataField("flush_sound", required: true)]
        private SoundSpecifier _flushSound = default!;

        [DataField("flush_time", required: true)]
        private float _flushTime;

        private Animation _flushAnimation = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            _flushAnimation = new Animation {Length = TimeSpan.FromSeconds(_flushTime)};

            var flick = new AnimationTrackSpriteFlick();
            _flushAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DisposalUnitVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(_stateFlush, 0));

            var sound = new AnimationTrackPlaySound();
            _flushAnimation.AnimationTracks.Add(sound);

            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(_flushSound.GetSound(), 0));
        }

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.TryGetData(Visuals.VisualState, out VisualState state))
            {
                return;
            }

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(appearance.Owner, out ISpriteComponent? sprite))
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

                    var animPlayer = entities.GetComponent<AnimationPlayerComponent>(appearance.Owner);

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

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            var entities = IoCManager.Resolve<IEntityManager>();
            entities.EnsureComponent<AnimationPlayerComponent>(entity);
            var appearance = entities.EnsureComponent<ClientAppearanceComponent>(entity);

            ChangeState(appearance);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
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

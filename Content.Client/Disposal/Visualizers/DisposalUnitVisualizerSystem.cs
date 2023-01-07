using Content.Client.Disposal.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.Visualizers
{
    public sealed class DisposalUnitVisualizerSystem : VisualizerSystem<DisposalUnitComponent>
    {
        [Dependency] private readonly SharedAudioSystem SoundSystem = default!;
        private const string AnimationKey = "disposal_unit_animation";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, DisposalUnitComponent disposalUnit, ComponentInit args)
        {
            disposalUnit.FlushAnimation = new Animation {
                Length = TimeSpan.FromSeconds(disposalUnit.FlushTime)
            };

            var flick = new AnimationTrackSpriteFlick();
            disposalUnit.FlushAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DisposalUnitVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(disposalUnit.StateFlush, 0));

            var sound = new AnimationTrackPlaySound();
            disposalUnit.FlushAnimation.AnimationTracks.Add(sound);

            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(SoundSystem.GetSound(disposalUnit.FlushSound), 0));

            EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);

            if (EntityManager.TryGetComponent<ISpriteComponent>(uid, out var sprite))
            {
                UpdateState(uid, disposalUnit, sprite);
            }
        }

        protected override void OnAppearanceChange(EntityUid uid, DisposalUnitComponent unit, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
            {
                return;
            }

            UpdateState(uid, unit, args.Sprite);
        }

        // Update visuals and tick animation
        private void UpdateState(EntityUid uid, DisposalUnitComponent unit, ISpriteComponent sprite)
        {
            if (!AppearanceSystem.TryGetData<VisualState>(uid, Visuals.VisualState, out var state))
            {
                return;
            }

            switch (state)
            {
                case VisualState.UnAnchored:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, unit.StateUnAnchored);
                    break;
                case VisualState.Anchored:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, unit.StateAnchored);
                    break;
                case VisualState.Charging:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, unit.StateCharging);
                    break;
                case VisualState.Flushing:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, unit.StateAnchored);

                    var animPlayer = EntityManager.GetComponent<AnimationPlayerComponent>(uid);

                    if (!AnimationSystem.HasRunningAnimation(uid, AnimationKey))
                    {
                        AnimationSystem.Play(uid, unit.FlushAnimation, AnimationKey);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!AppearanceSystem.TryGetData<HandleState>(uid, Visuals.Handle, out var handleState))
            {
                handleState = HandleState.Normal;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.Handle, handleState != HandleState.Normal);

            switch (handleState)
            {
                case HandleState.Normal:
                    break;
                case HandleState.Engaged:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Handle, unit.OverlayEngaged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!AppearanceSystem.TryGetData<LightState>(uid, Visuals.Light, out var lightState))
            {
                lightState = LightState.Off;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.Light, lightState != LightState.Off);

            switch (lightState)
            {
                case LightState.Off:
                    break;
                case LightState.Charging:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Light, unit.OverlayCharging);
                    break;
                case LightState.Full:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Light, unit.OverlayFull);
                    break;
                case LightState.Ready:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Light, unit.OverlayReady);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum DisposalUnitVisualLayers : byte
    {
        Base,
        Handle,
        Light
    }
}

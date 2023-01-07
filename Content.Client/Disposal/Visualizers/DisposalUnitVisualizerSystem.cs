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
            if (!EntityManager.TryGetComponent<ISpriteComponent>(uid, out var sprite))
            {
                return;
            }

            var flick = new AnimationTrackSpriteFlick { LayerKey = DisposalUnitVisualLayers.Base };
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(disposalUnit.StateFlush, 0));
            sprite.LayerMapTryGet(DisposalUnitVisualLayers.Base, out var layerIdx);
            var originalBaseState = sprite.LayerGetState(layerIdx);
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(originalBaseState, disposalUnit.FlushTime));

            var sound = new AnimationTrackPlaySound();
            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(SoundSystem.GetSound(disposalUnit.FlushSound), 0));

            disposalUnit.FlushAnimation = new Animation {
                Length = TimeSpan.FromSeconds(disposalUnit.FlushTime)
            };
            disposalUnit.FlushAnimation.AnimationTracks.Add(flick);
            disposalUnit.FlushAnimation.AnimationTracks.Add(sound);

            EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);

            UpdateState(uid, disposalUnit, sprite);
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

            sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == VisualState.UnAnchored);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == VisualState.Anchored);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseCharging, state == VisualState.Charging);

            if (state == VisualState.Flushing)
            {
                sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, true);

                var animPlayer = EntityManager.GetComponent<AnimationPlayerComponent>(uid);
                if (!AnimationSystem.HasRunningAnimation(uid, AnimationKey))
                {
                    AnimationSystem.Play(uid, unit.FlushAnimation, AnimationKey);
                }
            }

            if (!AppearanceSystem.TryGetData<HandleState>(uid, Visuals.Handle, out var handleState))
            {
                handleState = HandleState.Normal;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayEngaged, handleState != HandleState.Normal);

            if (!AppearanceSystem.TryGetData<LightState>(uid, Visuals.Light, out var lightState))
            {
                lightState = LightState.Off;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayCharging, lightState == LightState.Charging);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayReady, lightState == LightState.Ready);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayFull, lightState == LightState.Full);
        }
    }

    public enum DisposalUnitVisualLayers : byte
    {
        Unanchored,
        Base,
        BaseCharging,
        OverlayCharging,
        OverlayReady,
        OverlayFull,
        OverlayEngaged
    }
}

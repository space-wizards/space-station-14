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
            if (!EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            {
                return;
            }

            sprite.LayerMapTryGet(DisposalUnitVisualLayers.Base, out var baseLayerIdx);
            var originalBaseState = sprite.LayerGetState(baseLayerIdx);
            sprite.LayerMapTryGet(DisposalUnitVisualLayers.BaseFlush, out var flushLayerIdx);
            var flushState = sprite.LayerGetState(flushLayerIdx);

            // Setup the flush animation to play
            disposalUnit.FlushAnimation = new Animation {
                Length = TimeSpan.FromSeconds(disposalUnit.FlushTime),
                AnimationTracks = {
                    new AnimationTrackSpriteFlick {
                        LayerKey = DisposalUnitVisualLayers.BaseFlush,
                        KeyFrames = {
                            // Play the flush animation
                            new AnimationTrackSpriteFlick.KeyFrame(flushState, 0),
                            // Return to base state (though, depending on how the unit is
                            // configured we might get an appearence change event telling
                            // us to go to charging state)
                            new AnimationTrackSpriteFlick.KeyFrame(originalBaseState, disposalUnit.FlushTime)
                        }
                    },
                    new AnimationTrackPlaySound {
                        KeyFrames = {
                            new AnimationTrackPlaySound.KeyFrame(
                                    SoundSystem.GetSound(disposalUnit.FlushSound), 0)
                        }
                    }
                }
            };

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
        private void UpdateState(EntityUid uid, DisposalUnitComponent unit, SpriteComponent sprite)
        {
            if (!AppearanceSystem.TryGetData<VisualState>(uid, Visuals.VisualState, out var state))
            {
                return;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == VisualState.UnAnchored);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == VisualState.Anchored);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseCharging, state == VisualState.Charging);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseFlush, state == VisualState.Flushing);

            if (state == VisualState.Flushing)
            {
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

            if (!AppearanceSystem.TryGetData<LightStates>(uid, Visuals.Light, out var lightState))
            {
                lightState = LightStates.Off;
            }

            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayCharging,
                    (lightState & LightStates.Charging) != 0);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayReady,
                    (lightState & LightStates.Ready) != 0);
            sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayFull,
                    (lightState & LightStates.Full) != 0);
        }
    }

    public enum DisposalUnitVisualLayers : byte
    {
        Unanchored,
        Base,
        BaseCharging,
        BaseFlush,
        OverlayCharging,
        OverlayReady,
        OverlayFull,
        OverlayEngaged
    }
}

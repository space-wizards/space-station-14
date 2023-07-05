using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using static Content.Shared.Disposal.Components.DisposalUnitComponent;

namespace Content.Client.Disposal.Systems;

public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private const string AnimationKey = "disposal_unit_animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DisposalUnitComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(EntityUid uid, DisposalUnitComponent disposalUnit, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        UpdateState(uid, disposalUnit, sprite, appearance);
    }

    private void OnAppearanceChange(EntityUid uid, DisposalUnitComponent unit, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(uid, unit, args.Sprite, args.Component);
    }

    /// <summary>
    /// Update visuals and tick animation
    /// </summary>
    private void UpdateState(EntityUid uid, DisposalUnitComponent unit, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearanceSystem.TryGetData<VisualState>(uid, Visuals.VisualState, out var state, appearance))
        {
            return;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == VisualState.UnAnchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == VisualState.Anchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseCharging, state == VisualState.Charging);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseFlush, state == VisualState.Flushing);

        // This is a transient state so not too worried about replaying in range.
        if (state == VisualState.Flushing)
        {
            if (!_animationSystem.HasRunningAnimation(uid, AnimationKey) &&
                sprite.LayerMapTryGet(DisposalUnitVisualLayers.Base, out var baseLayerIdx) &&
                sprite.LayerMapTryGet(DisposalUnitVisualLayers.BaseFlush, out var flushLayerIdx))
            {
                var originalBaseState = sprite.LayerGetState(baseLayerIdx);
                var flushState = sprite.LayerGetState(flushLayerIdx);

                // Setup the flush animation to play
                var anim = new Animation
                {
                    Length = unit.FlushDelay,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = DisposalUnitVisualLayers.BaseFlush,
                            KeyFrames =
                            {
                                // Play the flush animation
                                new AnimationTrackSpriteFlick.KeyFrame(flushState, 0),
                                // Return to base state (though, depending on how the unit is
                                // configured we might get an appearance change event telling
                                // us to go to charging state)
                                new AnimationTrackSpriteFlick.KeyFrame(originalBaseState, (float) unit.FlushDelay.TotalSeconds)
                            }
                        },
                    }
                };

                if (unit.FlushSound != null)
                {
                    anim.AnimationTracks.Add(
                        new AnimationTrackPlaySound
                        {
                            KeyFrames =
                            {
                                new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(unit.FlushSound), 0)
                            }
                        });
                }

                _animationSystem.Play(uid, anim, AnimationKey);
            }
        }
        else
        {
            _animationSystem.Stop(uid, AnimationKey);
        }

        if (!_appearanceSystem.TryGetData<HandleState>(uid, Visuals.Handle, out var handleState, appearance))
        {
            handleState = HandleState.Normal;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayEngaged, handleState != HandleState.Normal);

        if (!_appearanceSystem.TryGetData<LightStates>(uid, Visuals.Light, out var lightState, appearance))
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

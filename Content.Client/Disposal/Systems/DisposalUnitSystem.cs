using Content.Client.Disposal.UI;
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

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<DisposalUnitComponent, ClientUserInterfaceComponent>();

        while (query.MoveNext(out var uid, out var comp, out var ui))
        {
            UpdateInterface(uid, comp, ui);
        }
    }

    private void UpdateInterface(EntityUid uid, DisposalUnitComponent component, ClientUserInterfaceComponent ui)
    {
        var state = component.UiState;

        if (state == null)
            return;

        foreach (var inter in ui.Interfaces)
        {
            if (inter is DisposalUnitBoundUserInterface boundInterface)
            {
                boundInterface.UpdateWindowState(state);
                return;
            }
        }
    }

    private void OnComponentInit(EntityUid uid, DisposalUnitComponent disposalUnit, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (!sprite.LayerMapTryGet(DisposalUnitVisualLayers.Base, out var baseLayerIdx))
            return; // Couldn't find the "normal" layer to return to after flush animation

        if (!sprite.LayerMapTryGet(DisposalUnitVisualLayers.BaseFlush, out var flushLayerIdx))
            return; // Couldn't find the flush animation layer

        var originalBaseState = sprite.LayerGetState(baseLayerIdx);
        var flushState = sprite.LayerGetState(flushLayerIdx);

        // Setup the flush animation to play
        disposalUnit.FlushAnimation = new Animation
        {
            Length = disposalUnit.FlushDelay,
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
                        new AnimationTrackSpriteFlick.KeyFrame(originalBaseState, (float) disposalUnit.FlushDelay.TotalSeconds)
                    }
                },
            }
        };

        if (disposalUnit.FlushSound != null)
        {
            disposalUnit.FlushAnimation.AnimationTracks.Add(
                new AnimationTrackPlaySound
                {
                    KeyFrames =
                    {
                        new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(disposalUnit.FlushSound), 0)
                    }
                });
        }

        EnsureComp<AnimationPlayerComponent>(uid);
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

        if (state == VisualState.Flushing)
        {
            // TODO: Need some kind of visual state to represent it
            // If we are in this state

            if (!_animationSystem.HasRunningAnimation(uid, AnimationKey))
            {
                _animationSystem.Play(uid, unit.FlushAnimation, AnimationKey);
            }
        }

        if (!_appearanceSystem.TryGetData<HandleState>(uid, Visuals.Handle, out var handleState))
        {
            handleState = HandleState.Normal;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayEngaged, handleState != HandleState.Normal);

        if (!_appearanceSystem.TryGetData<LightStates>(uid, Visuals.Light, out var lightState))
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

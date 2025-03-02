using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using static Content.Shared.Disposal.Components.DisposalUnitComponent;

namespace Content.Client.Disposal.Unit;

public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    private const string AnimationKey = "disposal_unit_animation";

    private const string DefaultFlushState = "disposal-flush";
    private const string DefaultChargeState = "disposal-charging";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<DisposalUnitComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnHandleState(EntityUid uid, DisposalUnitComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI((uid, component));
    }

    protected override void UpdateUI(Entity<DisposalUnitComponent> entity)
    {
        if (_uiSystem.TryGetOpenUi<DisposalUnitBoundUserInterface>(entity.Owner, DisposalUnitUiKey.Key, out var bui))
        {
            bui.Refresh(entity);
        }
    }

    protected override void OnDisposalInit(EntityUid uid, DisposalUnitComponent disposalUnit, ComponentInit args)
    {
        base.OnDisposalInit(uid, disposalUnit, args);

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
            return;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == VisualState.UnAnchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == VisualState.Anchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayFlush, state is VisualState.OverlayFlushing or VisualState.OverlayCharging);

        var chargingState = sprite.LayerMapTryGet(DisposalUnitVisualLayers.BaseCharging, out var chargingLayer)
            ? sprite.LayerGetState(chargingLayer)
            : new RSI.StateId(DefaultChargeState);

        // This is a transient state so not too worried about replaying in range.
        if (state == VisualState.OverlayFlushing)
        {
            if (!_animationSystem.HasRunningAnimation(uid, AnimationKey))
            {
                var flushState = sprite.LayerMapTryGet(DisposalUnitVisualLayers.OverlayFlush, out var flushLayer)
                    ? sprite.LayerGetState(flushLayer)
                    : new RSI.StateId(DefaultFlushState);

                // Setup the flush animation to play
                var anim = new Animation
                {
                    Length = unit.FlushDelay,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = DisposalUnitVisualLayers.OverlayFlush,
                            KeyFrames =
                            {
                                // Play the flush animation
                                new AnimationTrackSpriteFlick.KeyFrame(flushState, 0),
                                // Return to base state (though, depending on how the unit is
                                // configured we might get an appearance change event telling
                                // us to go to charging state)
                                new AnimationTrackSpriteFlick.KeyFrame(chargingState, (float) unit.FlushDelay.TotalSeconds)
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
                                new AnimationTrackPlaySound.KeyFrame(_audioSystem.ResolveSound(unit.FlushSound), 0)
                            }
                        });
                }

                _animationSystem.Play(uid, anim, AnimationKey);
            }
        }
        else if (state == VisualState.OverlayCharging)
            sprite.LayerSetState(DisposalUnitVisualLayers.OverlayFlush, chargingState);
        else
            _animationSystem.Stop(uid, AnimationKey);

        if (!_appearanceSystem.TryGetData<HandleState>(uid, Visuals.Handle, out var handleState, appearance))
            handleState = HandleState.Normal;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayEngaged, handleState != HandleState.Normal);

        if (!_appearanceSystem.TryGetData<LightStates>(uid, Visuals.Light, out var lightState, appearance))
            lightState = LightStates.Off;

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
    OverlayFlush,
    OverlayCharging,
    OverlayReady,
    OverlayFull,
    OverlayEngaged
}

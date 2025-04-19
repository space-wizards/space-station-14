using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Audio.Systems;

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
        if (_uiSystem.TryGetOpenUi<DisposalUnitBoundUserInterface>(entity.Owner, DisposalUnitComponent.DisposalUnitUiKey.Key, out var bui))
        {
            bui.Refresh(entity);
        }
    }

    protected override void OnDisposalInit(Entity<DisposalUnitComponent> ent, ref ComponentInit args)
    {
        base.OnDisposalInit(ent, ref args);

        if (!TryComp<SpriteComponent>(ent, out var sprite) || !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        UpdateState(ent, sprite, appearance);
    }

    private void OnAppearanceChange(Entity<DisposalUnitComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(ent, args.Sprite, args.Component);
    }

    /// <summary>
    /// Update visuals and tick animation
    /// </summary>
    private void UpdateState(Entity<DisposalUnitComponent> ent, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearanceSystem.TryGetData<DisposalUnitComponent.VisualState>(ent, DisposalUnitComponent.Visuals.VisualState, out var state, appearance))
            return;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == DisposalUnitComponent.VisualState.UnAnchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == DisposalUnitComponent.VisualState.Anchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayFlush, state is DisposalUnitComponent.VisualState.OverlayFlushing or DisposalUnitComponent.VisualState.OverlayCharging);

        var chargingState = sprite.LayerMapTryGet(DisposalUnitVisualLayers.BaseCharging, out var chargingLayer)
            ? sprite.LayerGetState(chargingLayer)
            : new RSI.StateId(DefaultChargeState);

        // This is a transient state so not too worried about replaying in range.
        if (state == DisposalUnitComponent.VisualState.OverlayFlushing)
        {
            if (!_animationSystem.HasRunningAnimation(ent, AnimationKey))
            {
                var flushState = sprite.LayerMapTryGet(DisposalUnitVisualLayers.OverlayFlush, out var flushLayer)
                    ? sprite.LayerGetState(flushLayer)
                    : new RSI.StateId(DefaultFlushState);

                // Setup the flush animation to play
                var anim = new Animation
                {
                    Length = ent.Comp.FlushDelay,
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
                                new AnimationTrackSpriteFlick.KeyFrame(chargingState, (float) ent.Comp.FlushDelay.TotalSeconds)
                            }
                        },
                    }
                };

                if (ent.Comp.FlushSound != null)
                {
                    anim.AnimationTracks.Add(
                        new AnimationTrackPlaySound
                        {
                            KeyFrames =
                            {
                                new AnimationTrackPlaySound.KeyFrame(_audioSystem.ResolveSound(ent.Comp.FlushSound), 0)
                            }
                        });
                }

                _animationSystem.Play(ent, anim, AnimationKey);
            }
        }
        else if (state == DisposalUnitComponent.VisualState.OverlayCharging)
            sprite.LayerSetState(DisposalUnitVisualLayers.OverlayFlush, chargingState);
        else
            _animationSystem.Stop(ent.Owner, AnimationKey);

        if (!_appearanceSystem.TryGetData<DisposalUnitComponent.HandleState>(ent, DisposalUnitComponent.Visuals.Handle, out var handleState, appearance))
            handleState = DisposalUnitComponent.HandleState.Normal;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayEngaged, handleState != DisposalUnitComponent.HandleState.Normal);

        if (!_appearanceSystem.TryGetData<DisposalUnitComponent.LightStates>(ent, DisposalUnitComponent.Visuals.Light, out var lightState, appearance))
            lightState = DisposalUnitComponent.LightStates.Off;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayCharging,
                (lightState & DisposalUnitComponent.LightStates.Charging) != 0);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayReady,
                (lightState & DisposalUnitComponent.LightStates.Ready) != 0);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayFull,
                (lightState & DisposalUnitComponent.LightStates.Full) != 0);
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

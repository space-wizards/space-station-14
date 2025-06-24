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
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.Unanchored, state == DisposalUnitComponent.VisualState.UnAnchored);
        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.Base, state == DisposalUnitComponent.VisualState.Anchored);
        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.OverlayFlush, state == DisposalUnitComponent.VisualState.OverlayFlushing);
        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.BaseCharging, state == DisposalUnitComponent.VisualState.OverlayCharging);

        var chargingState = _sprite.LayerMapTryGet((ent, sprite), DisposalUnitVisualLayers.BaseCharging, out var chargingLayer, false)
            ? _sprite.LayerGetRsiState((ent, sprite), chargingLayer)
            : new RSI.StateId(DefaultChargeState);

        // This is a transient state so not too worried about replaying in range.
        if (state == DisposalUnitComponent.VisualState.OverlayFlushing)
        {
            if (!_animationSystem.HasRunningAnimation(ent, AnimationKey))
            {
                var flushState = _sprite.LayerMapTryGet((ent, sprite), DisposalUnitVisualLayers.OverlayFlush, out var flushLayer, false)
                    ? _sprite.LayerGetRsiState((ent, sprite), flushLayer)
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
        else
            _animationSystem.Stop(ent.Owner, AnimationKey);

        if (!_appearanceSystem.TryGetData<DisposalUnitComponent.HandleState>(ent, DisposalUnitComponent.Visuals.Handle, out var handleState, appearance))
            handleState = DisposalUnitComponent.HandleState.Normal;

        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.OverlayEngaged, handleState != DisposalUnitComponent.HandleState.Normal);

        if (!_appearanceSystem.TryGetData<DisposalUnitComponent.LightStates>(ent, DisposalUnitComponent.Visuals.Light, out var lightState, appearance))
            lightState = DisposalUnitComponent.LightStates.Off;

        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.OverlayCharging,
                (lightState & DisposalUnitComponent.LightStates.Charging) != 0);
        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.OverlayReady,
                (lightState & DisposalUnitComponent.LightStates.Ready) != 0);
        _sprite.LayerSetVisible((ent, sprite), DisposalUnitVisualLayers.OverlayFull,
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

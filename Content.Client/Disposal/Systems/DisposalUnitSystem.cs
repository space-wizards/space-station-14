using System.Diagnostics.CodeAnalysis;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Events;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

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

        SubscribeLocalEvent<DisposalUnitComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<DisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<DisposalUnitComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, GotEmaggedEvent>(OnEmagged);

        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DisposalUnitComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnHandleState(EntityUid uid, DisposalUnitComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not DisposalUnitComponentState state)
            return;

        component.FlushSound = state.FlushSound;
        component.State = state.State;
        component.NextPressurized = state.NextPressurized;
        component.AutomaticEngageTime = state.AutomaticEngageTime;
        component.NextFlush = state.NextFlush;
        component.Powered = state.Powered;
        component.Engaged = state.Engaged;
        component.RecentlyEjected.Clear();
        component.RecentlyEjected.AddRange(EnsureEntityList<DisposalUnitComponent>(state.RecentlyEjected, uid));
    }

    public override bool HasDisposals(EntityUid? uid)
    {
        return HasComp<DisposalUnitComponent>(uid);
    }

    public override bool ResolveDisposals(EntityUid uid, [NotNullWhen(true)] ref SharedDisposalUnitComponent? component)
    {
        if (component != null)
            return true;

        TryComp<DisposalUnitComponent>(uid, out var storage);
        component = storage;
        return component != null;
    }

    public override void DoInsertDisposalUnit(EntityUid uid, EntityUid toInsert, EntityUid user, SharedDisposalUnitComponent? disposal = null)
    {
        return;
    }

    private void OnComponentInit(EntityUid uid, SharedDisposalUnitComponent sharedDisposalUnit, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        UpdateState(uid, sharedDisposalUnit, sprite, appearance);
    }

    private void OnAppearanceChange(EntityUid uid, SharedDisposalUnitComponent unit, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(uid, unit, args.Sprite, args.Component);
    }

    /// <summary>
    /// Update visuals and tick animation
    /// </summary>
    private void UpdateState(EntityUid uid, SharedDisposalUnitComponent unit, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearanceSystem.TryGetData<VisualState>(uid, Visuals.VisualState, out var state, appearance))
        {
            return;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == VisualState.UnAnchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == VisualState.Anchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseFlush, state is VisualState.Flushing or VisualState.Charging);

        // This is a transient state so not too worried about replaying in range.
        if (state == VisualState.Flushing)
        {
            if (!_animationSystem.HasRunningAnimation(uid, AnimationKey))
            {
                var flushState = new RSI.StateId("disposal-flush");

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
                                new AnimationTrackSpriteFlick.KeyFrame("disposal-charging", (float) unit.FlushDelay.TotalSeconds)
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
        else if (state == VisualState.Charging)
        {
            sprite.LayerSetState(DisposalUnitVisualLayers.BaseFlush, new RSI.StateId("disposal-charging"));
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

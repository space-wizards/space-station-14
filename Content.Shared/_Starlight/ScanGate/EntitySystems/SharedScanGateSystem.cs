using Content.Shared._Starlight.ScanGate.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;

namespace Content.Shared._Starlight.ScanGate.EntitySystems;

public sealed partial class SharedScanGateSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScanGateComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ScanDetectableComponent, TryDetectItem>(OnDetect);
        SubscribeLocalEvent<ScanDetectableComponent, InventoryRelayedEvent<TryDetectItem>>(OnInventoryRelay);
        Subs.SubscribeWithRelay<ScanDetectableComponent, HeldRelayedEvent<TryDetectItem>>(OnHandRelay, inventory: false);
        base.Initialize();
    }

    #region Logic
    private void OnCollide(EntityUid uid, ScanGateComponent component, ref StartCollideEvent args)
    {
        if (component.NextScanTime > _gameTiming.CurTime
            || !_powerReceiverSystem.IsPowered(uid))
            return;

        component.NextScanTime = _gameTiming.CurTime + component.ScanDelay;
        Dirty(uid, component);

        var ev = new TryDetectItem(uid);
        RaiseLocalEvent(args.OtherEntity, ref ev);

        if (ev.EntityDetected && !(TryComp<AccessReaderComponent>(uid, out var accessReader) && !_accessReaderSystem.IsAllowed(uid, args.OtherEntity, accessReader)))
            ItemDetected(uid, component); // Detected
        else
            NoItemDetected(uid, component); // Not detected
    }

    #endregion

    #region Detection

    /// <summary>
    /// An entity with <see cref="ScanDetectableComponent"/> has been detected by a scan gate.
    /// </summary>
    private void OnDetect(EntityUid uid, ScanDetectableComponent component, ref TryDetectItem args) => args.EntityDetected = true;

    /// <summary>
    /// An entity with <see cref="ScanDetectableComponent"/> has been detected by a scan gate.
    /// </summary>
    private void OnInventoryRelay(EntityUid uid, ScanDetectableComponent component, ref InventoryRelayedEvent<TryDetectItem> args) => args.Args.EntityDetected = true;

    /// <summary>
    /// An entity with <see cref="ScanDetectableComponent"/> has been detected by a scan gate.
    /// </summary>
    private void OnHandRelay(EntityUid uid, ScanDetectableComponent component, ref HeldRelayedEvent<TryDetectItem> args) => args.Args.EntityDetected = true;

    #endregion

    #region Actions
    private void ItemDetected(EntityUid uid, ScanGateComponent component)
    {
        _audio.PlayPvs(component.ScanFailSound, uid); // Play fail sound, when detect something
        SetState(uid, component, component.ScanFailState);
    }

    private void NoItemDetected(EntityUid uid, ScanGateComponent component)
    {
        _audio.PlayPvs(component.ScanSound, uid); // Play scan sound
        SetState(uid, component, component.ScanSuccessState);
    }

    private void SetState(EntityUid uid, ScanGateComponent component, string state)
    {
        _appearanceSystem.SetData(uid, ScanGateVisuals.State, state);
        Timer.Spawn(TimeSpan.FromSeconds(1), () => _appearanceSystem.SetData(uid, ScanGateVisuals.State, component.IdleState)); // Set back to idle after 1 second
    }
    #endregion
}
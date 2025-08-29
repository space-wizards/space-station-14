using Content.Shared._Starlight.ScanGate.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.PowerCell;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Storage;

namespace Content.Shared._Starlight.ScanGate.EntitySystems;

public sealed partial class SharedScanGateSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCellSystem = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScanGateComponent, StartCollideEvent>(OnCollide);

        // Detection events

        SubscribeLocalEvent<ScanDetectableComponent, TryDetectItem>(OnDetect);
        SubscribeLocalEvent<ScanDetectableComponent, InventoryRelayedEvent<TryDetectItem>>(OnInventoryRelay);
        Subs.SubscribeWithRelay<ScanDetectableComponent, HeldRelayedEvent<TryDetectItem>>(OnHandRelay, inventory: false);

        // Detect in storages

        SubscribeLocalEvent<StorageComponent, TryDetectItem>(OnDetectStorage);
        SubscribeLocalEvent<StorageComponent, InventoryRelayedEvent<TryDetectItem>>(OnInventoryRelayStorage); // Detect items in storage
        SubscribeLocalEvent<StorageComponent, HeldRelayedEvent<TryDetectItem>>(OnHandRelayStorage, inventory: false); // Detect items in storage

        // Bypass events

        SubscribeLocalEvent<ScanByPassComponent, TryDetectItem>(OnBypass);
        SubscribeLocalEvent<ScanByPassComponent, InventoryRelayedEvent<TryDetectItem>>(OnInventoryRelayBypass);
        Subs.SubscribeWithRelay<ScanByPassComponent, HeldRelayedEvent<TryDetectItem>>(OnHandRelayBypass, inventory: false);

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

        if (!ev.ByPass // Bypass detection if set
            && ev.EntityDetected
            && !(TryComp<AccessReaderComponent>(uid, out var accessReader) && _accessReaderSystem.IsAllowed(args.OtherEntity, uid, accessReader)))
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

    #region Storage Detection

    private void OnDetectStorage(EntityUid uid, StorageComponent component, ref TryDetectItem args)
    {
        if (args.ByPass) // No need to check if already bypassed
            return;

        foreach (var (entity, location) in component.StoredItems)
        {
            if (HasComp<ScanByPassComponent>(entity))
            {
                args.ByPass = true;
                break;
            }
            if (HasComp<ScanDetectableComponent>(entity))
                args.EntityDetected = true; // Keep checking, in case there's a bypass item
        }
    }

    private void OnInventoryRelayStorage(EntityUid uid, StorageComponent component, ref InventoryRelayedEvent<TryDetectItem> args)
    {
        if (args.Args.ByPass) // No need to check if already bypassed
            return;

        foreach (var (entity, location) in component.StoredItems)
        {
            if (HasComp<ScanByPassComponent>(entity))
            {
                args.Args.ByPass = true;
                break;
            }
            if (HasComp<ScanDetectableComponent>(entity))
                args.Args.EntityDetected = true;  // Keep checking, in case there's a bypass item
        }
    }

    private void OnHandRelayStorage(EntityUid uid, StorageComponent component, ref HeldRelayedEvent<TryDetectItem> args)
    {
        if (args.Args.ByPass) // No need to check if already bypassed
            return;

        foreach (var (entity, location) in component.StoredItems)
        {
            if (HasComp<ScanByPassComponent>(entity))
            {
                args.Args.ByPass = true;
                break;
            }
            if (HasComp<ScanDetectableComponent>(entity))
                args.Args.EntityDetected = true;  // Keep checking, in case there's a bypass item
        }
    }

    #endregion

    #region Bypass

    /// <summary>
    /// An entity with <see cref="ScanByPassComponent"/> is attempting to bypass scan gate detection.
    /// </summary>
    private void OnBypass(EntityUid uid, ScanByPassComponent component, ref TryDetectItem args)
    {
        if ((!component.Toggleable || (component.Toggleable && _itemToggleSystem.IsActivated(uid)))
            && (!component.Powered || (component.Powered && (_powerReceiverSystem.IsPowered(uid) || _powerCellSystem.HasDrawCharge(uid)))))
            args.ByPass = true;
    }

    /// <summary>
    /// An entity with <see cref="ScanByPassComponent"/> is attempting to bypass scan gate detection.
    /// </summary>
    private void OnInventoryRelayBypass(EntityUid uid, ScanByPassComponent component, ref InventoryRelayedEvent<TryDetectItem> args)
    {
        if ((!component.Toggleable || (component.Toggleable && _itemToggleSystem.IsActivated(uid)))
            && (!component.Powered || (component.Powered && (_powerReceiverSystem.IsPowered(uid) || _powerCellSystem.HasDrawCharge(uid)))))
            args.Args.ByPass = true;
    }

    /// <summary>
    /// An entity with <see cref="ScanByPassComponent"/> is attempting to bypass scan gate detection.
    /// </summary>
    private void OnHandRelayBypass(EntityUid uid, ScanByPassComponent component, ref HeldRelayedEvent<TryDetectItem> args)
    {
        if ((!component.Toggleable || (component.Toggleable && _itemToggleSystem.IsActivated(uid)))
            && (!component.Powered || (component.Powered && (_powerReceiverSystem.IsPowered(uid) || _powerCellSystem.HasDrawCharge(uid)))))
            args.Args.ByPass = true;
    }

    #endregion

    #region Actions

    /// <summary>
    /// Action which is performed when an item is detected by the scan gate.
    /// </summary>
    private void ItemDetected(EntityUid uid, ScanGateComponent component)
    {
        _audio.PlayPvs(component.ScanFailSound, uid); // Play fail sound, when detect something
        SetState(uid, component, component.ScanFailState);
        _deviceLink.InvokePort(uid, component.FailSignal);
    }

    /// <summary>
    /// Action which is performed when no item is detected by the scan gate.
    /// </summary>
    private void NoItemDetected(EntityUid uid, ScanGateComponent component)
    {
        _audio.PlayPvs(component.ScanSound, uid); // Play scan sound
        SetState(uid, component, component.ScanSuccessState);
        _deviceLink.InvokePort(uid, component.SuccessSignal);
    }

    /// <summary>
    /// Sets the visual state of the scan gate and resets it to idle after 1 second.
    /// </summary>
    /// <param name="uid"></param>
    private void SetState(EntityUid uid, ScanGateComponent component, string state)
    {
        _appearanceSystem.SetData(uid, ScanGateVisuals.State, state);
        Timer.Spawn(TimeSpan.FromSeconds(1), () => _appearanceSystem.SetData(uid, ScanGateVisuals.State, component.IdleState)); // Set back to idle after 1 second
    }
    #endregion
}
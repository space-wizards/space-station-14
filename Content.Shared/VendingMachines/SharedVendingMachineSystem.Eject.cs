using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Advertise.Components;
using Content.Shared.Advertise.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Emp;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Audio;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedSpeakOnUIClosedSystem _speakOn = default!;

    private void UpdateEjectState(Entity<VendingMachineComponent, VendingMachineEjectComponent> entity, TimeSpan curTime)
    {
        var eject = entity.Comp2;
        if (eject.EjectEnd is { } ejectEnd && curTime > ejectEnd)
        {
            eject.EjectEnd = null;
            Dirty(entity.Owner, eject);

            EjectItem((entity.Owner, entity.Comp1, eject));
            UpdateUI((entity.Owner, entity.Comp1));
            OnEjectStateChanged((entity.Owner, entity.Comp1), eject);
        }

        if (eject.DenyEnd is not { } denyEnd || curTime <= denyEnd)
            return;

        eject.DenyEnd = null;
        Dirty(entity.Owner, eject);

        OnEjectStateChanged((entity.Owner, entity.Comp1), eject);
    }

    private void OnInventoryEjectMessage(Entity<VendingMachineComponent> entity, ref VendingMachineEjectMessage args)
    {
        if (!_receiver.IsPowered(entity.Owner) || Deleted(entity))
            return;

        if (args.Actor is not { Valid: true } actor)
            return;

        AuthorizedVend(entity.Owner, actor, args.Type, args.ID, entity.Comp);
    }

    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<VendingMachineComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Broken || !_receiver.IsPowered(ent.Owner))
            return;

        if (!TryComp<VendingMachineEjectComponent>(ent.Owner, out var eject))
            return;

        args.Affected = true;
        args.Disabled = true;
        eject.NextEmpEject = Timing.CurTime;
    }

    protected virtual void EjectItem(Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity, bool forceEject = false) { }

    protected virtual void OnEjectStateChanged(Entity<VendingMachineComponent?> entity, VendingMachineEjectComponent? ejectComponent = null) { }

    protected virtual bool ShouldThrowVendItem(EntityUid uid, VendingMachineEjectComponent ejectComponent)
    {
        return false;
    }

    /// <summary>
    /// Checks if the user is authorized to use this vending machine
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="sender">Entity trying to use the vending machine</param>
    /// <param name="vendComponent"></param>
    public bool IsAuthorized(EntityUid uid, EntityUid sender, VendingMachineComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return false;

        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return true;

        if (_accessReader.IsAllowed(sender, uid, accessReader) || HasComp<EmaggedComponent>(uid))
            return true;

        Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender);
        Deny((uid, vendComponent), sender);
        return false;
    }

    protected VendingMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, InventoryType type, VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (type == InventoryType.Emagged && HasComp<EmaggedComponent>(uid))
            return component.EmaggedInventory.GetValueOrDefault(entryId);

        if (type == InventoryType.Contraband && component.Contraband)
            return component.ContrabandInventory.GetValueOrDefault(entryId);

        return component.Inventory.GetValueOrDefault(entryId);
    }

    /// <summary>
    /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
    /// or the item doesn't exist in its inventory.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="type">The type of inventory the item is from</param>
    /// <param name="itemId">The prototype ID of the item</param>
    /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
    /// <param name="user"></param>
    /// <param name="vendComponent"></param>
    /// <param name="ejectComponent"></param>
    public void TryEjectVendorItem(
        EntityUid uid,
        InventoryType type,
        string itemId,
        bool throwItem,
        EntityUid? user = null,
        VendingMachineComponent? vendComponent = null,
        VendingMachineEjectComponent? ejectComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return;

        if (!Resolve(uid, ref ejectComponent))
            return;

        if (ejectComponent.Ejecting || vendComponent.Broken || !_receiver.IsPowered(uid))
        {
            return;
        }

        var entry = GetEntry(uid, itemId, type, vendComponent);

        if (string.IsNullOrEmpty(entry?.ID))
        {
            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, uid);
            Deny((uid, vendComponent), ejectComponent: ejectComponent);
            return;
        }

        if (entry.Amount <= 0)
        {
            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, uid);
            Deny((uid, vendComponent), ejectComponent: ejectComponent);
            return;
        }

        // Start Ejecting and prevent users from ordering while anim playing
        ejectComponent.EjectEnd = Timing.CurTime + ejectComponent.EjectDelay;
        ejectComponent.NextItemToEject = entry.ID;
        ejectComponent.ThrowNextItem = throwItem;

        if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
            _speakOn.TrySetFlag((uid, speakComponent));

        entry.Amount--;
        Dirty(uid, vendComponent);
        Dirty(uid, ejectComponent);
        UpdateUI((uid, vendComponent));
        OnEjectStateChanged((uid, vendComponent), ejectComponent);
        Audio.PlayPredicted(ejectComponent.SoundVend, uid, user);
    }

    public void Deny(Entity<VendingMachineComponent?> entity, EntityUid? user = null, VendingMachineEjectComponent? ejectComponent = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (!Resolve(entity.Owner, ref ejectComponent))
            return;

        if (ejectComponent.Denying)
            return;

        ejectComponent.DenyEnd = Timing.CurTime + ejectComponent.DenyDelay;
        Audio.PlayPredicted(ejectComponent.SoundDeny, entity.Owner, user, AudioParams.Default.WithVolume(-2f));
        OnEjectStateChanged(entity, ejectComponent);
        Dirty(entity.Owner, ejectComponent);
    }

    /// <summary>
    /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="sender">Entity that is trying to use the vending machine</param>
    /// <param name="type">The type of inventory the item is from</param>
    /// <param name="itemId">The prototype ID of the item</param>
    /// <param name="component"></param>
    public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
    {
        if (!IsAuthorized(uid, sender, component)) return;

        if (!TryComp<VendingMachineEjectComponent>(uid, out var ejectComponent))
            return;

        TryEjectVendorItem(uid, type, itemId, ShouldThrowVendItem(uid, ejectComponent), sender, component, ejectComponent);
    }
}

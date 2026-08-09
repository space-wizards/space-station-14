using Content.Shared.Implants;
using Content.Shared.Mindshield;
using Content.Shared.Verbs;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    [SubscribeLocalEvent]
    public void RelayEvent<T>(Entity<InventoryComponent> inventory, ref T args) where T : IInventoryRelayEvent
    {
        if (args.TargetSlots == SlotFlags.NONE)
            return;

        var ev = new InventoryRelayedEvent<T>(args, inventory.Owner);
        var enumerator = new InventorySlotEnumerator(inventory, args.TargetSlots);
        while (enumerator.NextItem(out var item))
        {
            RaiseLocalEvent(item, ref ev);
        }
    }

    [SubscribeLocalEvent(after: [typeof(SharedSubdermalImplantSystem)])]
    private void RelayEventAfterImplant<T>(Entity<InventoryComponent> inventory, ref T args) where T : IInventoryRelayEvent, IInventoryRelayAfterImplantEvent
    {
        RelayEvent(inventory, ref args);
    }

    [SubscribeLocalEvent]
    private void OnGetEquipmentVerbs(Entity<InventoryComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        // Automatically relay stripping related verbs to all equipped clothing.
        var ev = new InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>(args, ent.Owner);
        var enumerator = new InventorySlotEnumerator(ent.Comp);
        while (enumerator.NextItem(out var item, out var slotDef))
        {
            if (!_strippable.IsStripHidden(slotDef, args.User) || args.User == ent.Owner)
                RaiseLocalEvent(item, ref ev);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetInnateVerbs(Entity<InventoryComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        // Automatically relay stripping related verbs to all equipped clothing.
        var ev = new InventoryRelayedEvent<GetVerbsEvent<InnateVerb>>(args, ent.Owner);
        var enumerator = new InventorySlotEnumerator(ent.Comp, SlotFlags.WITHOUT_POCKET);
        while (enumerator.NextItem(out var item))
        {
            RaiseLocalEvent(item, ref ev);
        }
    }
}

/// <summary>
///     Event wrapper for relayed events.
/// </summary>
/// <remarks>
///      This avoids nested inventory relays, and makes it easy to have certain events only handled by the initial
///      target entity. E.g. health based movement speed modifiers should not be handled by a hat, even if that hat
///      happens to be a dead mouse. Clothing that wishes to modify movement speed must subscribe to
///      InventoryRelayedEvent&lt;RefreshMovementSpeedModifiersEvent&gt;
/// </remarks>
[ByRefEvent]
public record struct InventoryRelayedEvent<TEvent>(TEvent Args, EntityUid Owner)
{
    public TEvent Args = Args;

    public EntityUid Owner = Owner;
}

public interface IClothingSlots
{
    SlotFlags Slots { get; }
}

/// <summary>
///     Events that should be relayed to inventory slots should implement this interface.
/// </summary>
public interface IInventoryRelayEvent
{
    /// <summary>
    ///     What inventory slots should this event be relayed to, if any?
    /// </summary>
    /// <remarks>
    ///     In general you may want to exclude <see cref="SlotFlags.POCKET"/>, given that those items are not truly
    ///     "equipped" by the user.
    /// </remarks>
    public SlotFlags TargetSlots { get; }
}

/// <summary>
/// Marker interface added to events that should run after <see cref="SharedSubdermalImplantSystem"/>.
/// </summary>
public interface IInventoryRelayAfterImplantEvent;

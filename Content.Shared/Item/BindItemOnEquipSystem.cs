using Content.Shared.Inventory.Events;

namespace Content.Shared.Item;

/// <summary>
/// Sends a <see cref="BindItemEvent"/> to the wearer of an item with
/// <see cref="BindItemOnEquipComponent"/>.
/// </summary>
public sealed partial class BindItemOnEquipSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEquipped(Entity<BindItemOnEquipComponent> ent, ref GotEquippedEvent args)
    {
        var ev = new BindItemEvent(ent.Owner);
        RaiseLocalEvent(args.EquipTarget, ref ev);
    }
}

/// <summary>
/// Raised on a wearer when an equipped item requests to be bound to them.
/// </summary>
/// <remarks>
/// The receiving system defines what binding means. This event only identifies the
/// <see cref="Item"/> and does not enforce ownership or replacement behavior.
/// </remarks>
[ByRefEvent]
public readonly record struct BindItemEvent(EntityUid Item);

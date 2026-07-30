using Content.Shared.Inventory.Events;

namespace Content.Shared.Item;

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
/// Raised on an entity when an equipped item asks to be bound to it.
/// </summary>
[ByRefEvent]
public readonly record struct BindItemEvent(EntityUid Item);

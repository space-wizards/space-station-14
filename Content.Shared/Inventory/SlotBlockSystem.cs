using Content.Shared.Inventory.Events;

namespace Content.Shared.Inventory;

/// <summary>
/// Handles prevention of items being unequipped and equipped from slots that are blocked by <see cref="SlotBlockComponent"/>.
/// </summary>
public sealed class SlotBlockSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<InventoryComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
    }

    private void OnEquipAttempt(Entity<InventoryComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var blocker = GetBlocker(ent, args.Slot);

        if (blocker == null)
            return;

        args.Reason = Loc.GetString("slot-block-component-blocked", ("item", blocker));
        args.Cancel();
    }

    private void OnUnequipAttempt(Entity<InventoryComponent> ent, ref IsUnequippingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var blocker = GetBlocker(ent, args.Slot);

        if (blocker == null)
            return;

        args.Reason = Loc.GetString("slot-block-component-blocked", ("item", blocker));
        args.Cancel();
    }

    private EntityUid? GetBlocker(Entity<InventoryComponent> ent, string slot)
    {
        foreach (var slotDef in ent.Comp.Slots)
        {
            if (!_inventorySystem.TryGetSlotEntity(ent, slotDef.Name, out var entity))
                continue;

            if (!TryComp<SlotBlockComponent>(entity, out var blockComponent) || Array.IndexOf(blockComponent.Slots, slot) == -1)
                continue;

            return entity;
        }

        return null;
    }
}

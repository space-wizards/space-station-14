using Content.Shared.ActionBlocker;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Inventory;

public sealed class SelfEquipOnlySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SelfEquipOnlyComponent, BeingEquippedAttemptEvent>(OnBeingEquipped);
        SubscribeLocalEvent<SelfEquipOnlyComponent, BeingUnequippedAttemptEvent>(OnBeingUnequipped);
    }

    private void OnBeingEquipped(Entity<SelfEquipOnlyComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<ClothingComponent>(ent, out var clothing) && (clothing.Slots & args.SlotFlags) == SlotFlags.NONE)
            return;

        if (args.Equipee != args.EquipTarget)
            args.Cancel();
    }

    private void OnBeingUnequipped(Entity<SelfEquipOnlyComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Unequipee == args.UnEquipTarget)
            return;

        if (TryComp<ClothingComponent>(ent, out var clothing) && (clothing.Slots & args.SlotFlags) == SlotFlags.NONE)
            return;

        if (ent.Comp.UnequipRequireConscious && !_actionBlocker.CanConsciouslyPerformAction(args.UnEquipTarget))
            return;
        args.Cancel();
    }
}

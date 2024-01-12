using Content.Shared.Interaction;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing.EntitySystems;
public sealed partial class AmpulaSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<AmpulaComponent, AfterInteractEvent>(OnAfterInteract);
    }
    private void OnAfterInteract(EntityUid uid, AmpulaComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;
        if (args.Handled)
            return;
        var target = args.Target;
        var user = args.User;
        var ampula = args.Used;
        var sys = _entManager.System<ItemSlotsSystem>();
        if (uid != ampula)
            return;
        if (!TryComp<InventoryComponent>(target, out var inventory))
            return;
        if (!_entManager.System<InventorySystem>().TryGetSlotEntity(target.Value, "outerClothing", out var slot, inventory))
            return;
        if (!TryComp<ItemSlotsComponent>(slot, out var itemslots))
            return;
        if (!TryComp<InjectComponent>(slot, out var containerlock))
            return;
        if (!sys.TryGetSlot(slot.Value, containerlock.ContainerId, out var itemslot, itemslots))
            return;
        if (!TryComp<HandsComponent>(user, out var handscomp))
            return;
        if (!itemslot.InsertOnInteract)
            return;

        if (!sys.CanInsert(slot.Value, args.Used, args.User, itemslot, swap: itemslot.Swap, popup: args.User))
            return;

        // Drop the held item onto the floor. Return if the user cannot drop.
        if (!_handsSystem.TryDrop(args.User, args.Used, handsComp: handscomp))
            return;

        if (itemslot.Item != null)
            _handsSystem.TryPickupAnyHand(args.User, itemslot.Item.Value, handsComp: handscomp);

        sys.TryInsert(slot.Value, itemslot, args.Used, user);
        args.Handled = true;
    }

}

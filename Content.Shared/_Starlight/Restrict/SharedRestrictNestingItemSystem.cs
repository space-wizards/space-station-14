using System.Linq;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Restrict;
public abstract partial class SharedRestrictNestingItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RestrictNestingItemComponent, PickupAttemptEvent>(OnAttemptItemPickup);
    }

    private void OnAttemptItemPickup(Entity<RestrictNestingItemComponent> ent, ref PickupAttemptEvent args)
    {
        //we need to recursively check inventory to see if the item being picked up has any other items that prevent nesting
        if (RecursivelyCheckForNesting(args.Item, true))
        {
            //if we find any, we need to cancel the pickup and show a popup message
            args.Cancel();
            ShowPopup(ent, args, "restrict-nesting-item-cant-pickup");
            return;
        }
    }

    private void ShowPopup(Entity<RestrictNestingItemComponent> ent, PickupAttemptEvent args, string reason)
    {
        // Popup logic.
        // Cooldown is needed because the input events for pickup will run multiple times at once
        if (!(_timing.CurTime > ent.Comp.NextPopupTime))
            return;

        _popup.PopupClient(Loc.GetString(reason), args.User, args.User);
        ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
    }

    private bool RecursivelyCheckForNesting(EntityUid item, bool initialItem = false)
    {
        //only do this check if the initial item is a mob. This allows duffelbags to work
        if (initialItem && !TryComp<MobMoverComponent>(item, out var mobMover))
            return false;

        //check if the item has the RestrictNestingItemComponent
        if (TryComp<RestrictNestingItemComponent>(item, out var nestingItem) && !initialItem)
            return true;

        //get the container of the item
        if (!TryComp<ContainerManagerComponent>(item, out var containerManager))
            return false;
        
        //now run this on all items in the inventory
        var containers = containerManager.GetAllContainers().ToList();
        var items = containers.SelectMany(container => container.ContainedEntities).ToList();

        foreach (var itemInInventory in items)
        {
            //run recursive check
            if (RecursivelyCheckForNesting(itemInInventory))
            {
                return true;
            }
        }

        //if we get here, we have no nesting items in the inventory
        return false;
    }
}

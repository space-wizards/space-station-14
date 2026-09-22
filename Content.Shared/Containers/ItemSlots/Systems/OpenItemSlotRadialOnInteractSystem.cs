using Content.Shared.Containers.ItemSlots.Components;
using Content.Shared.Containers.ItemSlots.Events;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Containers.ItemSlots.Systems;

/// <summary>
/// This takes care of opening radial menus when interacting with specific entities with item slots.
/// </summary>
public sealed partial class OpenItemSlotRadialOnInteractSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    [Dependency] private EntityQuery<ItemSlotsComponent> _itemSlotsQuery;

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<OpenItemSlotRadialOnInteractComponent> container, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_itemSlotsQuery.TryComp(container, out var slots))
            return;

        var user = args.User;
        var disabled = HasItemInAnySlot(slots);
        AlternativeVerb verb = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            Act = () => _ui.TryOpenUi(container.Owner, ItemSlotRadialUiKey.Key, user),
            Disabled = disabled,
            Message = disabled ? null : Loc.GetString("open-radial-menu-message"),
            Text = Loc.GetString("open-radial-menu-text"),
        };

        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnEmptyHandInteract(Entity<OpenItemSlotRadialOnInteractComponent> container, ref InteractHandEvent args)
    {
        if (args.Handled
            || container.Comp.AltInteraction
            ||!_itemSlotsQuery.TryComp(container, out var slots)
            || !HasItemInAnySlot(slots))
            return;

        args.Handled = _ui.TryOpenUi(container.Owner, ItemSlotRadialUiKey.Key, args.User);
    }

    [SubscribeLocalEvent]
    private void OnEjectMessage(Entity<OpenItemSlotRadialOnInteractComponent> container, ref EjectItemFromSlotMessage args)
    {
        if (!_itemSlots.TryGetSlot(container.Owner, args.SlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(container, slot, args.Actor, true);
    }

    private bool HasItemInAnySlot(ItemSlotsComponent slots)
    {
        foreach (var slot in slots.Slots.Values)
        {
            if (!slot.HasItem)
                continue;

            return true;
        }
        return false;
    }
}

[Serializable, NetSerializable]
public enum ItemSlotRadialUiKey : byte
{
    Key,
}

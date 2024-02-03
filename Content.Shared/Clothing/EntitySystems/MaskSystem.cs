using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class MaskSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaskComponent, ToggleMaskEvent>(OnToggleMask);
        SubscribeLocalEvent<MaskComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MaskComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGetActions(EntityUid uid, MaskComponent component, GetItemActionsEvent args)
    {
        if (!args.InHands)
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnToggleMask(Entity<MaskComponent> ent, ref ToggleMaskEvent args)
    {
        var (uid, mask) = ent;
        if (mask.ToggleActionEntity == null || !_timing.IsFirstTimePredicted)
            return;

        if (!_inventorySystem.TryGetSlotEntity(args.Performer, "mask", out var existing) || !uid.Equals(existing))
            return;

        mask.IsToggled ^= true;
        _actionSystem.SetToggled(mask.ToggleActionEntity, mask.IsToggled);

        if (mask.IsToggled)
            _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-down-popup-message", ("mask", uid)), args.Performer, args.Performer);
        else
            _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-up-popup-message", ("mask", uid)), args.Performer, args.Performer);

        ToggleMaskComponents(uid, mask, args.Performer);
    }

    // set to untoggled when unequipped, so it isn't left in a 'pulled down' state
    private void OnGotUnequipped(EntityUid uid, MaskComponent mask, GotUnequippedEvent args)
    {
        if (mask.ToggleActionEntity == null)
            return;

        mask.IsToggled = false;
        Dirty(uid, mask);
        _actionSystem.SetToggled(mask.ToggleActionEntity, mask.IsToggled);

        ToggleMaskComponents(uid, mask, args.Equipee, true);
    }

    private void ToggleMaskComponents(EntityUid uid, MaskComponent mask, EntityUid wearer, bool isEquip = false)
    {
        var maskEv = new ItemMaskToggledEvent(wearer, mask.IsToggled, isEquip);
        RaiseLocalEvent(uid, ref maskEv);

        var wearerEv = new WearerMaskToggledEvent(mask.IsToggled);
        RaiseLocalEvent(wearer, ref wearerEv);
    }
}

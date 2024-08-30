using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
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
        SubscribeLocalEvent<MaskComponent, FoldedEvent>(OnFolded);
    }

    private void OnGetActions(EntityUid uid, MaskComponent component, GetItemActionsEvent args)
    {
        if (_inventorySystem.InSlotWithFlags(uid, SlotFlags.MASK))
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnToggleMask(Entity<MaskComponent> ent, ref ToggleMaskEvent args)
    {
        var (uid, mask) = ent;

        if (mask.ToggleActionEntity == null || !_timing.IsFirstTimePredicted || !mask.IsEnabled)
            return;

        if (!_inventorySystem.TryGetSlotEntity(args.Performer, "mask", out var existing) || !uid.Equals(existing))
            return;

        mask.IsToggled ^= true;

        var dir = mask.IsToggled ? "down" : "up";
        var msg = $"action-mask-pull-{dir}-popup-message";

        _popupSystem.PopupEntity(Loc.GetString(msg, ("mask", uid)), args.Performer, args.Performer);

        ToggleMaskComponents(uid, mask, args.Performer);
    }

    // set to untoggled when unequipped, so it isn't left in a 'pulled down' state
    private void OnGotUnequipped(EntityUid uid, MaskComponent mask, GotUnequippedEvent args)
    {
        if (!mask.IsToggled || !mask.IsEnabled)
            return;

        mask.IsToggled = false;

        ToggleMaskComponents(uid, mask, args.Equipee);
    }

    /// <summary>
    /// Called after setting IsToggled, raises events and dirties.
    /// <summary>
    private void ToggleMaskComponents(EntityUid uid, MaskComponent mask, EntityUid wearer)
    {
        if (mask.ToggleActionEntity is { } action)
            _actionSystem.SetToggled(action, mask.IsToggled);

        var maskEv = new ItemMaskToggledEvent(wearer, mask.PulledDownPrefix, mask.IsToggled);
        RaiseLocalEvent(uid, ref maskEv);

        var wearerEv = new WearerMaskToggledEvent(mask.IsToggled);
        RaiseLocalEvent(wearer, ref wearerEv);
    }

    private void OnFolded(Entity<MaskComponent> ent, ref FoldedEvent args)
    {
        var (uid, comp) = ent;

        if (comp.DisableOnFolded)
            comp.IsEnabled = !args.IsFolded;

        comp.IsToggled = args.IsFolded;

        ToggleMaskComponents(uid, comp, uid);
    }
}

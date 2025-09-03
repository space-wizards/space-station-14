using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
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
    [Dependency] private readonly ClothingSystem _clothing = default!;

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
        if (mask.ToggleActionEntity == null || !mask.IsToggleable)
            return;

        // Masks are currently only toggleable via the action while equipped.
        // Its possible this might change in future?

        // TODO Inventory / Clothing
        // Add an easier way to check if clothing is equipped to a valid slot.
        if (!TryComp(ent, out ClothingComponent? clothing)
            || clothing.InSlotFlag is not { } slotFlag
            || !clothing.Slots.HasFlag(slotFlag))
        {
            return;
        }

        SetToggled((uid, mask), !mask.IsToggled);

        var dir = mask.IsToggled ? "down" : "up";
        var msg = $"action-mask-pull-{dir}-popup-message";
        _popupSystem.PopupClient(Loc.GetString(msg, ("mask", uid)), args.Performer, args.Performer);
    }

    private void OnGotUnequipped(EntityUid uid, MaskComponent mask, GotUnequippedEvent args)
    {
        // Masks are currently always un-toggled when unequipped.
        SetToggled((uid, mask), false);
    }

    private void OnFolded(Entity<MaskComponent> ent, ref FoldedEvent args)
    {
        // See FoldableClothingComponent

        if (!ent.Comp.DisableOnFolded)
            return;

        // While folded, we force the mask to be toggled / pulled down, so that its functionality as a mask is disabled,
        // and we also prevent it from being un-toggled. We also automatically untoggle it when it gets unfolded, so it
        // fully returns to its previous state when folded & unfolded.

        SetToggled(ent!, args.IsFolded, force: true);
        SetToggleable(ent!, !args.IsFolded);
    }

    public void SetToggled(Entity<MaskComponent?> mask, bool toggled, bool force = false)
    {
        if (_timing.ApplyingState)
            return;

        if (!Resolve(mask.Owner, ref mask.Comp))
            return;

        if (!force && !mask.Comp.IsToggleable)
            return;

        if (mask.Comp.IsToggled == toggled)
            return;

        mask.Comp.IsToggled = toggled;

        if (mask.Comp.ToggleActionEntity is { } action)
            _actionSystem.SetToggled(action, mask.Comp.IsToggled);

        // TODO Generalize toggling & clothing prefixes. See also FoldableClothingComponent
        var prefix = mask.Comp.IsToggled ? mask.Comp.EquippedPrefix : null;
        _clothing.SetEquippedPrefix(mask, prefix);

        // TODO Inventory / Clothing
        // Add an easier way to get the entity that is wearing clothing in a valid slot.
        EntityUid? wearer = null;
        if (TryComp(mask, out ClothingComponent? clothing)
            && clothing.InSlotFlag is {} slotFlag
            && clothing.Slots.HasFlag(slotFlag))
        {
            wearer = Transform(mask).ParentUid;
        }

        var maskEv = new ItemMaskToggledEvent(mask!, wearer);
        RaiseLocalEvent(mask, ref maskEv);

        if (wearer != null)
        {
            var wearerEv = new WearerMaskToggledEvent(mask!);
            RaiseLocalEvent(wearer.Value, ref wearerEv);
        }

        Dirty(mask);
    }

    public void SetToggleable(Entity<MaskComponent?> mask, bool toggleable)
    {
        if (_timing.ApplyingState)
            return;

        if (!Resolve(mask.Owner, ref mask.Comp))
            return;

        if (mask.Comp.IsToggleable == toggleable)
            return;

        if (mask.Comp.ToggleActionEntity is { } action)
            _actionSystem.SetEnabled(action, mask.Comp.IsToggleable);

        mask.Comp.IsToggleable = toggleable;
        Dirty(mask);
    }
}

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
        if (mask.ToggleActionEntity == null || !mask.IsEnabled)
            return;

        // TODO Inventory / Clothing
        // Add an easier way to check if clothing is equipped to a valid slot.
        if (!TryComp(ent, out ClothingComponent? clothing)
            || clothing.InSlot is not { } slot
            || !clothing.Slots.HasFlag(slot.Flag))
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
        // Masks are always un-toggled when unequipped
        if (mask.IsToggled)
            SetToggled((uid, mask), false);
    }

    private void OnFolded(Entity<MaskComponent> ent, ref FoldedEvent args)
    {
        if (ent.Comp.DisableOnFolded)
            SetEnabled(ent!, !args.IsFolded);

        SetToggled(ent!, args.IsFolded);
    }

    public void SetToggled(Entity<MaskComponent?> mask, bool toggled)
    {
        if (_timing.ApplyingState)
            return;

        if (!Resolve(mask.Owner, ref mask.Comp))
            return;

        if (mask.Comp.IsToggled == toggled)
            return;

        mask.Comp.IsToggled = toggled;

        if (mask.Comp.ToggleActionEntity is { } action)
            _actionSystem.SetToggled(action, mask.Comp.IsToggled);

        // TODO Generalize toggling & clothing prefixes
        var prefix = mask.Comp.IsToggled ? mask.Comp.EquippedPrefix : null;
        _clothing.SetEquippedPrefix(mask, prefix);

        // TODO Inventory / Clothing
        // Add an easier way to get the entity that is wearing clothing in a valid slot.
        EntityUid? wearer = null;
        if (TryComp(mask, out ClothingComponent? clothing)
            && clothing.InSlot is {} slot
            && clothing.Slots.HasFlag(slot.Flag))
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

    public void SetEnabled(Entity<MaskComponent?> mask, bool enabled)
    {
        if (_timing.ApplyingState)
            return;

        if (!Resolve(mask.Owner, ref mask.Comp))
            return;

        if (mask.Comp.IsEnabled == enabled)
            return;

        mask.Comp.IsEnabled = enabled;
        Dirty(mask);
    }
}

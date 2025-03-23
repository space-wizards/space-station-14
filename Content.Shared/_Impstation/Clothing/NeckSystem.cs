using Content.Shared._Impstation.Clothing;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Clothing;

public sealed class NeckSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeckComponent, ToggleNeckEvent>(OnToggleNeck);
        SubscribeLocalEvent<NeckComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<NeckComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<NeckComponent, FoldedEvent>(OnFolded);
    }

    private void OnGetActions(EntityUid uid, NeckComponent component, GetItemActionsEvent args)
    {
        if (_inventorySystem.InSlotWithFlags(uid, SlotFlags.NECK))
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnToggleNeck(Entity<NeckComponent> ent, ref ToggleNeckEvent args)
    {
        var (uid, neck) = ent;
        if (neck.ToggleActionEntity == null || !_timing.IsFirstTimePredicted || !neck.IsEnabled)
            return;

        if (!_inventorySystem.TryGetSlotEntity(args.Performer, "neck", out var existing) || !uid.Equals(existing))
            return;

        neck.IsToggled ^= true;

        var dir = neck.IsToggled ? "down" : "up";
        var msg = $"action-neck-pull-{dir}-popup-message";
        _popupSystem.PopupClient(Loc.GetString(msg, ("neck", uid)), args.Performer, args.Performer);

        ToggleNeckComponents(uid, neck, args.Performer, neck.EquippedPrefix);
    }

    // set to untoggled when unequipped, so it isn't left in a 'pulled down' state
    private void OnGotUnequipped(EntityUid uid, NeckComponent neck, GotUnequippedEvent args)
    {
        if (!neck.IsToggled || !neck.IsEnabled)
            return;

        neck.IsToggled = false;
        ToggleNeckComponents(uid, neck, args.Equipee, neck.EquippedPrefix, true);
    }

    /// <summary>
    /// Called after setting IsToggled, raises events and dirties.
    /// <summary>
    private void ToggleNeckComponents(EntityUid uid, NeckComponent neck, EntityUid wearer, string? equippedPrefix = null, bool isEquip = false)
    {
        Dirty(uid, neck);
        if (neck.ToggleActionEntity is {} action)
            _actionSystem.SetToggled(action, neck.IsToggled);

        var neckEv = new ItemNeckToggledEvent(wearer, equippedPrefix, neck.IsToggled, isEquip);
        RaiseLocalEvent(uid, ref neckEv);

        var wearerEv = new WearerNeckToggledEvent(neck.IsToggled);
        RaiseLocalEvent(wearer, ref wearerEv);
    }

    private void OnFolded(Entity<NeckComponent> ent, ref FoldedEvent args)
    {
        if (ent.Comp.DisableOnFolded)
            ent.Comp.IsEnabled = !args.IsFolded;
        ent.Comp.IsToggled = args.IsFolded;

        ToggleNeckComponents(ent.Owner, ent.Comp, ent.Owner);
    }
}

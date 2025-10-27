using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles adding and using a toggle action for <see cref="ToggleClothingComponent"/>.
/// </summary>
public sealed class ToggleClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleClothingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ToggleClothingComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ToggleClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnMapInit(Entity<ToggleClothingComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        // test funny
        if (string.IsNullOrEmpty(comp.Action))
            return;

        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
        _actions.SetToggled(comp.ActionEntity, _toggle.IsActivated(ent.Owner));
        Dirty(uid, comp);
    }

    private void OnGetActions(Entity<ToggleClothingComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands && ent.Comp.MustEquip)
            return;

        var ev = new ToggleClothingCheckEvent(args.User);
        RaiseLocalEvent(ent, ref ev);

        if (!ev.Cancelled)
            args.AddAction(ent.Comp.ActionEntity);
    }

    private void OnToggleAction(Entity<ToggleClothingComponent> ent, ref ToggleActionEvent args)
    {
        args.Handled = _toggle.Toggle(ent.Owner, args.Performer);
    }

    private void OnUnequipped(Entity<ToggleClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (ent.Comp.DisableOnUnequip)
            _toggle.TryDeactivate(ent.Owner, args.Wearer);
    }
}

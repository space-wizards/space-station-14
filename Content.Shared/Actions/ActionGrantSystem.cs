using Content.Shared.Actions.Components;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared.Actions;

/// <summary>
/// <see cref="ActionGrantComponent"/>
/// </summary>
public sealed partial class ActionGrantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void OnItemGet(Entity<ItemActionGrantComponent> ent, ref GetItemActionsEvent args)
    {
        if (!TryComp(ent.Owner, out ActionGrantComponent? grant))
            return;

        if (ent.Comp.ActiveIfWorn && (args.SlotFlags == null || args.SlotFlags == SlotFlags.POCKET))
            return;

        foreach (var action in grant.ActionEntities)
        {
            if (TryComp<ActionUserWhitelistComponent>(action, out var whitelist) &&
                !_whitelist.IsWhitelistPass(whitelist.Whitelist, args.User))
            {
                continue;
            }

            args.AddAction(action);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ActionGrantComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            EntityUid? actionEnt = null;
            _actions.AddAction(ent.Owner, ref actionEnt, action);

            if (actionEnt != null)
                ent.Comp.ActionEntities.Add(actionEnt.Value);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ActionGrantComponent> ent, ref ComponentShutdown args)
    {
        foreach (var actionEnt in ent.Comp.ActionEntities)
        {
            _actions.RemoveAction(ent.Owner, actionEnt);
        }
    }
}

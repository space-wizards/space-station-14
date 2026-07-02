using Content.Shared.Cloning.Events;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

/// <summary>
/// <see cref="ActionGrantComponent"/>
/// </summary>
public sealed partial class ActionGrantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActionGrantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ActionGrantComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionGrantComponent, CloningEvent>(OnClone);
        SubscribeLocalEvent<ItemActionGrantComponent, GetItemActionsEvent>(OnItemGet);
    }

    private void OnItemGet(Entity<ItemActionGrantComponent> ent, ref GetItemActionsEvent args)
    {

        if (!TryComp(ent.Owner, out ActionGrantComponent? grant))
            return;

        if (ent.Comp.ActiveIfWorn && (args.SlotFlags == null || args.SlotFlags == SlotFlags.POCKET))
            return;

        foreach (var action in grant.ActionEntities)
        {
            args.AddAction(action);
        }
    }

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

    private void OnClone(Entity<ActionGrantComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        var cloneComp = Factory.GetComponent<ActionGrantComponent>();
        cloneComp.Actions = new List<EntProtoId>(ent.Comp.Actions);
        AddComp(args.CloneUid, cloneComp, true);
    }

    private void OnShutdown(Entity<ActionGrantComponent> ent, ref ComponentShutdown args)
    {
        foreach (var actionEnt in ent.Comp.ActionEntities)
        {
            _actions.RemoveAction(ent.Owner, actionEnt);
        }
    }
}

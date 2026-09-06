using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Map;

namespace Content.Server.NPC.Systems;

public sealed partial class NpcUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            if (!action.Ref)
                action.ActionEnt = _actions.AddAction(ent, action.ActionId);
        }
    }

    [SubscribeLocalEvent]
    private void OnAddedAction(Entity<NPCUseActionOnTargetComponent> entity, ref AddedActionEvent args)
    {
        var protoId = MetaData(args.Action.Owner).EntityPrototype;
        Log.Debug($"NPC: {ToPrettyString(entity)} has added an action {ToPrettyString(args.Action)}.");
        foreach (var action in entity.Comp.Actions)
        {
            // Don't try to add an action, if we already have one or if it's the wrong prototype
            if (!action.Ref || protoId?.ID != action.ActionId.Id)
                continue;

            action.ActionEnt = args.Action;
            action.Ref = false;
        }
    }

    [SubscribeLocalEvent]
    private void OnRemovedAction(Entity<NPCUseActionOnTargetComponent> entity, ref RemovedActionEvent args)
    {
        foreach (var action in entity.Comp.Actions)
        {
            if (action.ActionEnt != args.Action.Owner)
                continue;

            action.ActionEnt = null;
            action.Ref = true;
        }
    }

    private bool TryUseAction(Entity<NPCUseActionOnTargetComponent?> user, NpcActionData action, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        if (action.ActionEnt is { } actionEnt)
            return _actions.TryPerformAction(user.Owner, actionEnt, target, Transform(target).Coordinates, false);

        Log.Error($"An NPC attempted to perform an action without an action!");
        return false;

    }

    public override void Update(float frameTime)
    {
        // TODO: TryUseAction should be called by the NPC directly rather than trying to use an action every tick.
        base.Update(frameTime);

        // Tries to use the attack on the current target.
        var query = EntityQueryEnumerator<NPCUseActionOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            foreach (var action in comp.Actions)
            {
                if (action.Ref || !htn.Blackboard.TryGetValue<EntityUid>(action.TargetKey, out var target, EntityManager))
                    continue;

                // Only use one action per tick
                if (TryUseAction((uid, comp), action, target))
                    return;
            }
        }
    }
}

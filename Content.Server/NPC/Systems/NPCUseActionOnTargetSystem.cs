using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Map;

namespace Content.Server.NPC.Systems;

public sealed class NpcUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionOnTargetComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NPCUseActionOnTargetComponent, AddedActionEvent>(OnAddedAction);
        SubscribeLocalEvent<NPCUseActionOnTargetComponent, RemovedActionEvent>(OnRemovedAction);
        SubscribeLocalEvent<WorldTargetActionComponent, ValidateNpcTargetEvent>(OnNpcWorldTarget);
        SubscribeLocalEvent<EntityTargetActionComponent, ValidateNpcTargetEvent>(OnNpcEntityTarget);
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            if (!action.Ref)
                action.ActionEnt = _actions.AddAction(ent, action.ActionId) ?? null;
        }
    }

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

        if (action.ActionEnt is not {} actionEnt)
        {
            Log.Error($"An NPC attempted to perform an action without an action!");
            return false;
        }

        var ev = new ValidateNpcTargetEvent(target);
        RaiseLocalEvent(actionEnt, ref ev);
        if (ev.Invalid)
            return false;

        return _actions.TryPerformAction(user.Owner, actionEnt, ev.EntTarget, ev.EntityCoordinates, false);
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

    private void OnNpcWorldTarget(Entity<WorldTargetActionComponent> entity, ref ValidateNpcTargetEvent ev)
    {
        ev.EntityCoordinates = Transform(ev.Target).Coordinates;
    }

    private void OnNpcEntityTarget(Entity<EntityTargetActionComponent> entity, ref ValidateNpcTargetEvent ev)
    {
        ev.EntTarget = ev.Target;
    }
}

[ByRefEvent]
public struct ValidateNpcTargetEvent(EntityUid target)
{
    public readonly EntityUid Target = target;

    public bool Invalid;
    public EntityUid? EntTarget;
    public EntityCoordinates? EntityCoordinates;
}

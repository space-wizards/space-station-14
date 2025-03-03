using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionOnTargetComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NPCUseActionOnTargetComponent, ActionAddedEvent>(OnActionAdded);
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            if (action.Reference)
                //TODO: This should have logic to find the action or create a listener for if the action gets added but currently does nothing
                action.ActionEnt = _actions.AddAction(ent, action.ActionId);
            else
                action.ActionEnt = _actions.AddAction(ent, action.ActionId);
        }
    }

    private void OnActionAdded(EntityUid uid, NPCUseActionOnTargetComponent component, ActionAddedEvent args)
    {
        Log.Error($"Rawr!");
    }

    public void TryUseAction(Entity<NPCUseActionOnTargetComponent?> user, Components.NPCActionsData action, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return;

        if (action.ActionEnt is not { Valid: true } entityTarget)
        {
            Log.Error($"An NPC attempted to perform an entity-targeted action without a target!");
            return;
        }

        _actions.IsValidAction(user.Owner, entityTarget, target, Transform(target).Coordinates);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Tries to use the attack on the current target.
        var query = EntityQueryEnumerator<NPCUseActionOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            foreach (var action in comp.Actions)
            {
                if (!htn.Blackboard.TryGetValue<EntityUid>(action.TargetKey, out var target, EntityManager))
                    continue;

                TryUseAction((uid, comp), action, target);
            }
        }
    }
}

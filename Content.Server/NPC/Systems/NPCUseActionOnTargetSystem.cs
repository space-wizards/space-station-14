using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Robust.Shared.Map;
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
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        //check if an action already exists first here to avoid duplicating actions
        //register more than one action, probably use a for each action in ent.Comp.NPCActions
        /*foreach (var action in ent.Comp.Actions)
        {
            action.ActionEnt = _actions.AddAction(ent, action.ActionId);
        }*/
        ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);
    }

    public void TryUseAction(Entity<NPCUseActionOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return;

        if (user.Comp.ActionEnt is not { Valid: true } entityTarget)
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
            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager))
                continue;

            TryUseAction((uid, comp), target);
        }
    }
}

using Content.Server.NPC.HTN;
using Content.Shared.Abilities.Goliath;
using Content.Shared.Abilities.Goliath.Components;
using Content.Shared.Actions;
using Robust.Shared.Timing;

namespace Content.Server.Abilities.Goliath;

public sealed class GoliathTentacleSystem : SharedGoliathTentacleSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoliathTentacleUserComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GoliathTentacleUserComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.TentacleAction = _actions.AddAction(ent, ent.Comp.TentacleActionId);
    }

    /// <summary>
    /// Function that handles all the action logic manually. Used for NPC interactions.
    /// </summary>
    public bool TryUseTentacleAttack(Entity<GoliathTentacleUserComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        if (!TryComp<EntityWorldTargetActionComponent>(user.Comp.TentacleAction, out var action))
            return false;

        if (!_actions.ValidAction(action))
            return false;

        if (action.Event != null)
        {
            action.Event.Performer = user;
            action.Event.Action = user.Comp.TentacleAction.Value;
            action.Event.Coords = Transform(target).Coordinates;
        }

        _actions.PerformAction(user,
            null,
            user.Comp.TentacleAction.Value,
            action,
            action.BaseEvent,
            _timing.CurTime,
            false);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Tries to use the attack on the current target.
        var query = EntityQueryEnumerator<GoliathTentacleUserComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var goliath, out var htn))
        {
            if (!htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager))
                continue;

            TryUseTentacleAttack((uid, goliath), target);
        }
    }
}

using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.NPC.Systems;

public sealed class NPCUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionOnTargetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        foreach(var action in ent.Comp.Actions)
        {
            var key = action.Key;
            var value = action.Value;

            var actionEnt = _actions.AddAction(ent, key);

            if (actionEnt is null)
                return;

            ent.Comp.ActionsEntities[actionEnt.Value] = value;
        }
    }

    public bool TryUseAction(Entity<NPCUseActionOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        var choosenAction = ChooseAction(user.Comp.ActionsEntities);

        if (!TryComp<EntityWorldTargetActionComponent>(choosenAction, out var action))
            return false;

        if (!_actions.ValidAction(action))
            return false;

        if (action.Event != null)
        {
            action.Event.Coords = Transform(target).Coordinates;
        }

        _actions.PerformAction(user,
            null,
            choosenAction,
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
        var query = EntityQueryEnumerator<NPCUseActionOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager))
                continue;

            TryUseAction((uid, comp), target);
        }
    }

    private EntityUid ChooseAction(Dictionary<EntityUid, float> actions)
    {
        List<EntityUid> keys = actions.Keys.ToList();
        List<float> values = actions.Values.ToList();
        EntityUid defaultToReturn = keys[values.IndexOf(values.Max())];

        for (int i = 0; i < actions.Count; i++)
        {
            float maxProb = values.Max();
            int maxindex = values.IndexOf(maxProb);
            if (_random.NextFloat() <= maxProb)
                return keys[maxindex];

            values.RemoveAt(maxindex);
            keys.RemoveAt(maxindex);
        }
        return defaultToReturn;
    }
}

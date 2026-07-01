using Content.Server.Objectives.Components;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions.Mind;
using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Server.EntityConditions.Conditions;

/// <summary>
/// Checks if the target entity, is an objective target of the given source mind entity.
/// Then filters by a whitelist, if any objectives pass the whitelist and target entity is a target, the condition passes.
/// Fails if the passed argument is null or not a mind.
/// </summary>
public sealed partial class ObjectiveTargetEntityConditionSystem : EntityConditionSystem<MindComponent, ObjectiveTargetCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private EntityQuery<TargetObjectiveComponent> _targetQuery;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<ObjectiveTargetCondition> args)
    {
        if (!TryComp<MindComponent>(args.SourceEnt, out var mind))
            return;

        foreach (var objective in mind.Objectives)
        {
            // if the player has an objective targeting this mind
            if (!_targetQuery.TryComp(objective, out var kill) || kill.Target != entity)
                continue;

            // remove the mind if this objective is blacklisted
            if (!_whitelist.IsWhitelistPassOrNull(args.Condition.Whitelist, objective))
                continue;

            args.Result = true;
            return;
        }
    }
}

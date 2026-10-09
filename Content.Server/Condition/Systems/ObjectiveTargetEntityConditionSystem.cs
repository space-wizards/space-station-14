using Content.Server.Objectives.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Server.Condition.Systems;

/// <summary>
/// Checks if the target entity, is an objective target of the given source mind entity.
/// Then filters by a whitelist, if any objectives pass the whitelist and target entity is a target, the condition passes.
/// Fails if the passed argument is null or not a mind.
/// </summary>
public sealed partial class ObjectiveTargetEntityConditionSystem : ConditionEvaluatorSystem<IObjectiveTargetCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private EntityQuery<TargetObjectiveComponent> _targetQuery;


    public override float Evaluate(IObjectiveTargetCondition condition,
        EntityUid entityUid,
        EntityUid? sourceEntity = null)
    {
        if (!TryComp<MindComponent>(sourceEntity, out var mind))
            return 0;

        if (mind.Objectives.Count == 0)
            return 0;

        float value = 0;
        foreach (var objective in mind.Objectives)
        {
            // if the player has an objective targeting this mind
            if (!_targetQuery.TryComp(objective, out var kill) || kill.Target != entityUid)
                continue;

            // remove the mind if this objective is blacklisted
            if (!_whitelist.IsWhitelistPassOrNull(condition.Whitelist, objective))
                continue;

            value++;
        }

        return value / mind.Objectives.Count;
    }
}

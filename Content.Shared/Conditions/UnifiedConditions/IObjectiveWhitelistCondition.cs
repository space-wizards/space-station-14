using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IObjectiveWhitelistCondition : ICondition
{
    EntityWhitelist? Whitelist { get; }

    EntityWhitelist? Blacklist { get; }
}

/// <summary>
/// Checks if the target mind has an objective which passes the given whitelist and/or blacklist.
/// </summary>
public sealed partial class ObjectiveEntityConditionSystem : ConditionEvaluatorSystem<IObjectiveWhitelistCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override float Evaluate(IObjectiveWhitelistCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out MindComponent? mindComponent))
            return 0;
        float value = 0;
        foreach (var obj in mindComponent.Objectives)
        {
            // mind has a blacklisted objective, remove it from the pool
            if (!_whitelist.CheckBoth(obj, condition.Blacklist, condition.Whitelist))
                continue;
            //count hits, as to get a scale.
            value++;
        }
        return value/mindComponent.Objectives.Count;
    }
}

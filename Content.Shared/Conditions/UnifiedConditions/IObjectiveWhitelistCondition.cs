using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IObjectiveWhitelistCondition : ICondition<IObjectiveWhitelistCondition>
{
    EntityWhitelist? Whitelist { get; }

    EntityWhitelist? Blacklist { get; }
}

/// <summary>
/// Checks if the target mind has an objective which passes the given whitelist and/or blacklist.
/// </summary>
public sealed partial class ObjectiveEntityConditionSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private void Condition(Entity<MindComponent> entity,
        ref ConditionEvaluationEvent<IObjectiveWhitelistCondition> args)
    {
        args.Handled = true;

        foreach (var obj in entity.Comp.Objectives)
        {
            // mind has a blacklisted objective, remove it from the pool
            if (!_whitelist.CheckBoth(obj, args.Condition.Blacklist, args.Condition.Whitelist))
                continue;
            //count hits, as to get a scale.
            args.Value++;
        }

        args.Value /= entity.Comp.Objectives.Count;
    }
}

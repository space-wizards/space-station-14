using Content.Shared.Conditions;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// Checks if the target mind has an objective which passes the given whitelist and/or blacklist.
/// </summary>
public sealed partial class ObjectiveEntityConditionSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not ObjectiveCondition condition)
            return;

        args.Handled = true;

        foreach (var obj in entity.Comp.Objectives)
        {
            // mind has a blacklisted objective, remove it from the pool
            if (!_whitelist.CheckBoth(obj, condition.Blacklist, condition.Whitelist))
                continue;
            //count hits, as to get a scale.
            args.Value++;
        }

        args.Value /= (float)entity.Comp.Objectives.Count;
    }
}

public sealed partial class ObjectiveCondition : EntityConditionBase<ObjectiveCondition>
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}


using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// Checks if the target mind has an objective which passes the given whitelist and/or blacklist.
/// </summary>
public sealed partial class ObjectiveWhitelistEntityConditionSystem : EntityConditionSystem<MindComponent, ObjectiveWhitelistCondition>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<ObjectiveWhitelistCondition> args)
    {
        foreach (var obj in entity.Comp.Objectives)
        {
            // mind has a blacklisted objective, remove it from the pool
            if (!_whitelist.CheckBoth(obj, args.Condition.Blacklist, args.Condition.Whitelist))
                continue;

            args.Result = true;
            return;
        }
    }
}

public sealed partial class ObjectiveWhitelistCondition : EntityConditionBase<ObjectiveWhitelistCondition>
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


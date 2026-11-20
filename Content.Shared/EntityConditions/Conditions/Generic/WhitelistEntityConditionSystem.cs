using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

public sealed partial class WhitelistEntityConditionSystem : EntityConditionSystem<MetaDataComponent, WhitelistCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<WhitelistCondition> args)
    {
        args.Result = _whitelist.CheckBoth(entity, args.Condition.Blacklist, args.Condition.Whitelist);
    }
}

/// <summary>
/// Checks whether an entity belongs to a whitelist/blacklist.
/// </summary>
public sealed partial class WhitelistCondition : EntityConditionBase<WhitelistCondition>
{
    /// <summary>
    /// The whitelist this entity needs to belong to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The blacklist this entity cannot belong to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        // I seriously doubt this is possible to sanely fill out based on a whitelist/blacklist.
        // "Target must not have HumanoidProfile" type shit.
        return String.Empty;
    }
}


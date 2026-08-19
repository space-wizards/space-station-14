using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class BodyWhitelistEntityConditionSystem : EntityConditionSystem<MindComponent, BodyWhitelistCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<BodyWhitelistCondition> args)
    {
        if (entity.Comp.OwnedEntity is not { } body)
            return;

        args.Result = _whitelist.CheckBoth(body, args.Condition.Blacklist, args.Condition.Whitelist);
    }
}

public sealed partial class BodyWhitelistCondition : EntityConditionBase<BodyWhitelistCondition>
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


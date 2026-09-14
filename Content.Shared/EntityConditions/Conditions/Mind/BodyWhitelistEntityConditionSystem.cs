using Content.Shared.Conditions;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class BodyWhitelistEntityConditionSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not BodyWhitelistCondition condition)
            return;
        args.Handled = true;
        if (entity.Comp.OwnedEntity is not { } body)
            return;

        args.Value = _whitelist.CheckBoth(body, condition.Blacklist, condition.Whitelist) ? 1 : 0;
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

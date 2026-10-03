using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IBodyWhitelistCondition : ICondition
{
    EntityWhitelist? Whitelist { get; }

    EntityWhitelist? Blacklist { get; }
}

public sealed partial class BodyWhitelistConditionSystem : ConditionEvaluatorSystem<IBodyWhitelistCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override float Evaluate(IBodyWhitelistCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
       return _whitelist.CheckBoth(entityUid, condition.Blacklist, condition.Whitelist) ? 1 : 0;
    }
}

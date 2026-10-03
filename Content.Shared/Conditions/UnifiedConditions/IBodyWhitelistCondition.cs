using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IBodyWhitelistCondition : ICondition<IBodyWhitelistCondition>
{
    EntityWhitelist? Whitelist { get; }

    EntityWhitelist? Blacklist { get; }
}

public sealed partial class BodyWhitelistConditionSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<IBodyWhitelistCondition> args)
    {
        args.Handled = true;

        if (entity.Comp.OwnedEntity is not { } body)
            return;

        args.Value = _whitelist.CheckBoth(body, args.Condition.Blacklist, args.Condition.Whitelist) ? 1 : 0;
    }
}

using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IRoleCondition : ICondition<IRoleCondition>
{
    EntityWhitelist Whitelist { get; }
}

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs,
/// or is jobless.
/// </summary>
public sealed partial class MindContainerRoleEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindContainerComponent> entity, ref ConditionEvaluationEvent<IRoleCondition> args)
    {
        args.Handled = true;

        if (!TryComp<MindComponent>(entity.Comp.Mind, out var mind))
            return;

        args.Value = _role.MindHasRole((entity.Comp.Mind.Value, mind), args.Condition.Whitelist) ? 1 : 0;
    }

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<IRoleCondition> args)
    {
        args.Handled = true;
        args.Value = _role.MindHasRole(entity, args.Condition.Whitelist) ? 1 : 0;
    }
}

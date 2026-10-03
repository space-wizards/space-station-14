using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IRoleCondition : ICondition
{
    EntityWhitelist Whitelist { get; }
}

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs,
/// or is jobless.
/// </summary>
public sealed partial class MindContainerRoleEntityConditionSystem : ConditionEvaluatorSystem<IRoleCondition>
{
    [Dependency] private SharedRoleSystem _role = default!;

    public override float Evaluate(IRoleCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (TryComp(entityUid, out MindContainerComponent? containerComponent)&&containerComponent.Mind!=null)
        {
            entityUid = containerComponent.Mind.Value;
        }

        if (!TryComp(entityUid, out MindComponent? mind))
            return 0;

        return _role.MindHasRole((entityUid, mind), condition.Whitelist) ? 1 : 0;
    }
}

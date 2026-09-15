using Content.Shared.Conditions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class AntagonistEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<AntagonistCondition> args)
    {
        args.Handled = true;

        args.Value = _roleSystem.MindIsAntagonist(entity) ? 1 : 0;
    }
}

/// <summary>
/// Checks if the given mind is an antagonist.
/// </summary>
public sealed partial class AntagonistCondition : EntityConditionBase<AntagonistCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}

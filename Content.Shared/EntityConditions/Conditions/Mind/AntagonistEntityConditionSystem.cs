using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class AntagonistEntityConditionSystem : EntityConditionSystem<MindComponent, AntagonistCondition>
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<AntagonistCondition> args)
    {
        args.Result = _roleSystem.MindIsAntagonist(entity);
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


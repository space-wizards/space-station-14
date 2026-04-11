using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class AntagonistEntityConditionSystem : EntityConditionSystem<MindComponent, AntagonistEntityCondition>
{
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<AntagonistEntityCondition> args)
    {
        args.Result = _roleSystem.MindIsAntagonist(entity);
    }
}

/// <summary>
/// Checks if the given mind is an antagonist.
/// </summary>
public sealed partial class AntagonistEntityCondition : EntityConditionBase<AntagonistEntityCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}


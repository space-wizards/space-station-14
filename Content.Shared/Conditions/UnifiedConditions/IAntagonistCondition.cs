using Content.Shared.Mind;
using Content.Shared.Roles;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IAntagonistCondition : ICondition
{
}

public sealed partial class AntagonistEntityConditionSystem : ConditionEvaluatorSystem<IAntagonistCondition>
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    public override float Evaluate(IAntagonistCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return _roleSystem.MindIsAntagonist(entityUid) ? 1 : 0;
    }
}

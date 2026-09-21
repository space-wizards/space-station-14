using Content.Shared.Mind;
using Content.Shared.Roles;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IAntagonistCondition : ICondition<IAntagonistCondition>
{
}

public sealed partial class AntagonistEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<IAntagonistCondition> args)
    {
        args.Handled = true;

        args.Value = _roleSystem.MindIsAntagonist(entity) ? 1 : 0;
    }
}

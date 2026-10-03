using Content.Shared.Body.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IInternalsCondition : ICondition
{
}

/// <summary>
/// Returns true if this entity is using internals. False if they are not or cannot use internals.
/// </summary>
public sealed partial class InternalsOnEntityConditionSystem : ConditionEvaluatorSystem<IInternalsCondition>
{
    public override float Evaluate(IInternalsCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out InternalsComponent? internalsComponent))
            return 0;
        return internalsComponent.GasTankEntity != null ? 1 : 0;
    }
}

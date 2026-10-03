using System.Linq;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IMetabolizerTypeCondition : ICondition
{
    /// <summary>
    /// Which metabolizer types would fulfill this condition. Need only one match.
    /// </summary>
    ProtoId<MetabolizerTypePrototype>[] Type { get; }
}

/// <summary>
/// Returns true if this entity has any of the listed metabolizer types.
/// </summary>
public sealed partial class MetabolizerTypeConditionSystem : ConditionEvaluatorSystem<IMetabolizerTypeCondition>
{
    public override float Evaluate(IMetabolizerTypeCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out MetabolizerComponent? component))
            return 0;

        if (component.MetabolizerTypes == null||condition.Type.Length==0)
            return 0;

        return component.MetabolizerTypes.Intersect(condition.Type).Count() /
                     (float)condition.Type.Length;
    }
}

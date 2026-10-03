using System.Linq;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IMetabolizerTypeCondition : ICondition<IMetabolizerTypeCondition>
{
    /// <summary>
    /// Which metabolizer types would fulfill this condition. Need only one match.
    /// </summary>
    ProtoId<MetabolizerTypePrototype>[] Type { get; }
}

/// <summary>
/// Returns true if this entity has any of the listed metabolizer types.
/// </summary>
public sealed partial class MetabolizerTypeConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MetabolizerComponent> entity,
        ref ConditionEvaluationEvent<IMetabolizerTypeCondition> args)
    {
        args.Handled = true;
        if (entity.Comp.MetabolizerTypes == null)
            return;

        args.Value = entity.Comp.MetabolizerTypes.Intersect(args.Condition.Type).Count() /
                     (float)args.Condition.Type.Length;
    }
}

using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

///<summary>
/// Evaluator for <see cref="TemperatureCondition"/>
/// </summary>
public sealed partial class TemplateEntityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not TemplateCondition)
            return;
        // Condition goes here.
        args.Handled = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TemplateCondition : EntityConditionBase<TemplateCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}

using Content.Shared.Conditions;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Examine;

namespace Content.Shared.Construction
{
    /// <summary>
    /// Base for all Graph conditions, to be stored in data structures.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class GraphCondition : IConditionByEvent
    {
        public abstract bool DoExamine(ExaminedEvent args);
        public abstract IEnumerable<ConstructionGuideEntry> GenerateGuideEntry();

        [DataField]
        public Satisfier? Satisfier { get; set; }

        public abstract ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity);
    }

    /// <summary>
    /// Helper class to properly realize the Condition Event.
    /// </summary>
    /// <typeparam name="TCondition"></typeparam>
    public abstract partial class GraphConditionBase<TCondition> : GraphCondition, IConditionByEvent<TCondition>
        where TCondition : IConditionByEvent
    {
        public override ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity)
        {
            if (this is not TCondition condition)
                return null;
            return new ConditionEvaluationEvent<TCondition>(condition, entity, sourceEntity);
        }
    }
}

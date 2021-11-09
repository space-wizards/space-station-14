using Content.Server.Body.Metabolism;
using Content.Shared.Body.Metabolism;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires that the metabolizing organ is or is not tagged with a certain MetabolizerType
    /// </summary>
    public class OrganType : ReagentEffectCondition
    {
        [DataField("type", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MetabolizerTypePrototype>))]
        public string Type = default!;

        /// <summary>
        ///     Does this condition pass when the organ has the type, or when it doesn't have the type?
        /// </summary>
        [DataField("shouldHave")]
        public bool ShouldHave = true;

        public override bool Condition(EntityUid solutionEntity, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            if (entityManager.TryGetComponent<MetabolizerComponent>(organEntity, out var metabolizer)
                && metabolizer.MetabolizerTypes != null
                && metabolizer.MetabolizerTypes.Contains(Type))
                return ShouldHave;
            return !ShouldHave;
        }
    }
}

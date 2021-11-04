using Content.Server.Body.Metabolism;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires that the metabolizing organ is or is not tagged with a certain MetabolismType
    /// </summary>
    public class OrganType : ReagentEffectCondition
    {
        [DataField("type", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MetabolismTypePrototype>))]
        public string Type = default!;

        [DataField("shouldHave")]
        public bool ShouldHave = true;

        public override bool Condition(IEntity solutionEntity, IEntity organEntity, Solution.ReagentQuantity reagent)
        {
            if (organEntity.TryGetComponent<MetabolizerComponent>(out var metabolizer)
                && metabolizer.MetabolizerTypes != null
                && metabolizer.MetabolizerTypes.Contains(Type))
                return ShouldHave;
            return !ShouldHave;
        }
    }
}

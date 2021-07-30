using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Used for implementing reagent effects that require a certain amount of reagent before it should be applied.
    ///     For instance, overdoses.
    /// </summary>
    public class ReagentThreshold : ReagentEffectCondition
    {
        [DataField("min")]
        public ReagentUnit Min = ReagentUnit.Zero;

        [DataField("max")]
        public ReagentUnit Max = ReagentUnit.MaxValue;

        public override bool Condition(IEntity solutionEntity, Solution.ReagentQuantity reagent)
        {
            return reagent.Quantity >= Min && reagent.Quantity < Max;
        }
    }
}

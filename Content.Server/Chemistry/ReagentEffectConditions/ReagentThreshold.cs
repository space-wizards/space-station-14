using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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
        public FixedPoint2 Min = FixedPoint2.Zero;

        [DataField("max")]
        public FixedPoint2 Max = FixedPoint2.MaxValue;

        public override bool Condition(EntityUid solutionEntity, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            return reagent.Quantity >= Min && reagent.Quantity < Max;
        }
    }
}

using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    /// <summary>
    ///     Reagent effects describe behavior that occurs when a reagent is ingested and metabolized by some
    ///     organ. They only trigger when their conditions (<see cref="ReagentEffectCondition"/>
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class ReagentEffect
    {
        /// <summary>
        ///     The list of conditions required for the effect to activate. Not required.
        /// </summary>
        [DataField("conditions")]
        public ReagentEffectCondition[]? Conditions;

        public abstract void Metabolize(IEntity solutionEntity, Solution.Solution.ReagentQuantity amount);
    }
}

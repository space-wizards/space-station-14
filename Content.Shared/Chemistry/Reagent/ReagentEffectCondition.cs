using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class ReagentEffectCondition
    {
        public abstract bool Condition(IEntity solutionEntity, Solution.Solution.ReagentQuantity reagent);
    }
}

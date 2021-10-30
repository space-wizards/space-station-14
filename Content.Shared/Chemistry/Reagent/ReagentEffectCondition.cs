using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class ReagentEffectCondition
    {
        public abstract bool Condition(IEntity solutionEntity, Solution.ReagentQuantity reagent);
    }
}

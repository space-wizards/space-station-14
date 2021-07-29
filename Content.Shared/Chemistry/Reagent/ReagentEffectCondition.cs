using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class ReagentEffectCondition
    {
        public abstract bool Condition(IEntity solutionEntity, ReagentUnit amount);
    }
}

using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class ReagentEffectCondition
    {
        public abstract bool Condition(ReagentEffectArgs args);
    }
}

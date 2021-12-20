using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class ReagentEffectCondition
    {
        [JsonProperty("id")] private string _id => this.GetType().Name;

        public abstract bool Condition(ReagentEffectArgs args);
    }
}

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class ReagentEffectCondition
    {
        [JsonPropertyName("id")] private protected string _id => this.GetType().Name;

        public abstract bool Condition(ReagentEffectArgs args);
    }
}

using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reagent
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract partial class ReagentEffectCondition
    {
        [JsonPropertyName("id")] private protected string _id => this.GetType().Name;

        public abstract bool Condition(ReagentEffectArgs args);

        /// <summary>
        /// Effect explanations are of the form "[chance to] [action] when [condition] and [condition]"
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public abstract string GuidebookExplanation(IPrototypeManager prototype);
    }
}

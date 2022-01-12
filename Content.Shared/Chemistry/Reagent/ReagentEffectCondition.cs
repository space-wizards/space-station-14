using System.Text.Json.Serialization;
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
        [JsonPropertyName("id")] private string _id => this.GetType().Name;

        public abstract bool Condition(ReagentEffectArgs args);
    }
}

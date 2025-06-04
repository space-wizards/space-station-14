using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class EntityEffectCondition
{
    [JsonPropertyName("id")] private protected string _id => this.GetType().Name;

    public abstract bool Condition(EntityEffectBaseArgs args);

    /// <summary>
    /// Effect explanations are of the form "[chance to] [action] when [condition] and [condition]"
    /// </summary>
    /// <param name="prototype"></param>
    /// <returns></returns>
    public abstract string GuidebookExplanation(IPrototypeManager prototype);
}

[ByRefEvent]
public struct CheckEntityEffectConditionEvent<T> where T : EntityEffectCondition
{
    public T Condition;
    public EntityEffectBaseArgs Args;
    public bool Result;
}

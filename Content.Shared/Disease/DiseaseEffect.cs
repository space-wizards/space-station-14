using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Content.Shared.Disease
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class DiseaseEffect
    {
        [JsonPropertyName("id")] private protected string _id => this.GetType().Name;

        public abstract void Effect(DiseaseEffectArgs args);
    }
    public readonly record struct DiseaseEffectArgs(
        EntityUid DiseasedEntity,
        DiseasePrototype Disease
    );
}

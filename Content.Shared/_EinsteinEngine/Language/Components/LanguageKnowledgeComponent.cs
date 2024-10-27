using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._EinsteinEngine.Language.Components;

/// <summary>
///     Stores data about entities' intrinsic language knowledge.
/// </summary>
[RegisterComponent]
public sealed partial class LanguageKnowledgeComponent : Component
{
    /// <summary>
    ///     List of languages this entity can speak without any external tools.
    /// </summary>
    [DataField("speaks", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>), required: true)]
    public List<string> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand without any external tools.
    /// </summary>
    [DataField("understands", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>), required: true)]
    public List<string> UnderstoodLanguages = new();
}

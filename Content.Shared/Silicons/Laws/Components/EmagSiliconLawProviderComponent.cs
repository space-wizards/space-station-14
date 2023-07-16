using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity that only grants laws when emagged.
/// </summary>
[RegisterComponent]
public sealed class EmagSiliconLawProviderComponent : Component
{
    /// <summary>
    /// The laws that are provided.
    /// </summary>
    [DataField("laws", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<SiliconLawPrototype>))]
    public List<string> Laws = new();
}

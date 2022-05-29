using Content.Shared.Dataset;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.RandomMetadata;

/// <summary>
///     Randomizes the description and/or the name for an entity by pulling from a dataset prototype.
/// </summary>
[RegisterComponent]
public sealed class RandomMetadataComponent : Component
{
    [DataField("descriptionSet", customTypeSerializer:typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string? DescriptionSet;

    [DataField("nameSet", customTypeSerializer:typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string? NameSet;
}

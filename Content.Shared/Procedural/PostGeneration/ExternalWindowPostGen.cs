using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If external areas are found will try to generate windows.
/// </summary>
public sealed partial class ExternalWindowPostGen : IPostDunGen
{
    [DataField("entities", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string?> Entities = new()
    {
        "Grille",
        "Window",
    };

    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";
}

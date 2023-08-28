using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If internal areas are found will try to generate windows.
/// </summary>
public sealed partial class InternalWindowPostGen : IPostDunGen
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

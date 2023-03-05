using Content.Server._Citadel.Worldgen.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Citadel.Worldgen.Components;

/// <summary>
///     This is used for controlling overall world loading, containing an index of all chunks in the map.
/// </summary>
[RegisterComponent]
[Access(typeof(WorldControllerSystem))]
public sealed class WorldControllerComponent : Component
{
    [DataField("chunkProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ChunkProto = "CitadelChunk";

    /// <summary>
    ///     An index of chunks owned by the controller.
    /// </summary>
    [DataField("chunks")] public Dictionary<Vector2i, EntityUid> Chunks = new();
}


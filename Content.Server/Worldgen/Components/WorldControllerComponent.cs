using Content.Server.Worldgen.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Components;

/// <summary>
///     This is used for controlling overall world loading, containing an index of all chunks in the map.
/// </summary>
[RegisterComponent]
[Access(typeof(WorldControllerSystem))]
public sealed partial class WorldControllerComponent : Component
{
    /// <summary>
    ///     The prototype to use for chunks on this world map.
    /// </summary>
    [DataField]
    public EntProtoId ChunkProto = "WorldChunk";

    /// <summary>
    ///     An index of chunks owned by the controller.
    /// </summary>
    [DataField("chunks")] public Dictionary<Vector2i, EntityUid> Chunks = new();
}


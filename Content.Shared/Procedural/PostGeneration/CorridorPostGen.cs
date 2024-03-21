using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Connects room entrances via corridor segments.
/// </summary>
public sealed partial class CorridorPostGen : IPostDunGen
{
    /// <summary>
    /// How far we're allowed to generate a corridor before calling it.
    /// </summary>
    /// <remarks>
    /// Given the heavy weightings this needs to be fairly large for larger dungeons.
    /// </remarks>
    [DataField("pathLimit")]
    public int PathLimit = 2048;

    [DataField("method")]
    public CorridorPostGenMethod Method = CorridorPostGenMethod.MinimumSpanningTree;

    [DataField]
    public ProtoId<ContentTileDefinition> Tile = "FloorSteel";

    /// <summary>
    /// How wide to make the corridor.
    /// </summary>
    [DataField("width")]
    public int Width = 3;
}

public enum CorridorPostGenMethod : byte
{
    Invalid,
    MinimumSpanningTree,
}

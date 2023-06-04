namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Connects room entrances via corridor segments.
/// </summary>
public sealed class CorridorPostGen : IPostDunGen
{
    [DataField("method")]
    public CorridorPostGenMethod Method = CorridorPostGenMethod.MinimumSpanningTree;

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

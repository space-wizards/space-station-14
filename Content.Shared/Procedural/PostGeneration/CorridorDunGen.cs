namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Connects room entrances via corridor segments.
/// </summary>
/// <remarks>
/// Dungeon data keys are:
/// - FallbackTile
/// </remarks>
public sealed partial class CorridorDunGen : IDunGenLayer
{
    /// <summary>
    /// How far we're allowed to generate a corridor before calling it.
    /// </summary>
    /// <remarks>
    /// Given the heavy weightings this needs to be fairly large for larger dungeons.
    /// </remarks>
    [DataField]
    public int PathLimit = 2048;

    /// <summary>
    /// How wide to make the corridor.
    /// </summary>
    [DataField]
    public float Width = 3f;
}

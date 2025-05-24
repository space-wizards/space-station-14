namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places the specified entities at junction areas.
/// </summary>
/// <remarks>
/// Dungeon data keys are:
/// - Entrance
/// - FallbackTile
/// </remarks>
public sealed partial class JunctionDunGen : IDunGenLayer
{
    /// <summary>
    /// Width to check for junctions.
    /// </summary>
    [DataField]
    public int Width = 3;
}

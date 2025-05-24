namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Iterates room edges and places the relevant tiles and walls on any free indices.
/// </summary>
/// <remarks>
/// Dungeon data keys are:
/// - CornerWalls (Optional)
/// - FallbackTile
/// - Walls
/// </remarks>
public sealed partial class BoundaryWallDunGen : IDunGenLayer
{
    [DataField]
    public BoundaryWallFlags Flags = BoundaryWallFlags.Corridors | BoundaryWallFlags.Rooms;
}

[Flags]
public enum BoundaryWallFlags : byte
{
    Rooms = 1 << 0,
    Corridors = 1 << 1,
}

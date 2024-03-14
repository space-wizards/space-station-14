using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

// Ime a worm
/// <summary>
/// Generates worm corridors first then places rooms on the ends.
/// </summary>
public sealed partial class WormDunGen : IDunGen
{
    /// <summary>
    /// How many rooms to place.
    /// </summary>
    [DataField]
    public int RoomCount = 5;

    /// <summary>
    /// How many times to run the worm
    /// </summary>
    [DataField]
    public int Count = 10;

    /// <summary>
    /// How long to make each worm
    /// </summary>
    [DataField]
    public int Length = 30;

    /// <summary>
    /// Maximum amount the angle can change in a single step.
    /// </summary>
    [DataField]
    public Angle MaxAngleChange;

    [DataField]
    public ProtoId<ContentTileDefinition> Tile = "FloorSteel";

    /// <summary>
    /// How wide to make the corridor.
    /// </summary>
    [DataField]
    public int Width = 3;
}

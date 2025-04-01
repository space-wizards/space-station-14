namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places tiles / entities onto room entrances.
/// </summary>
/// <remarks>
/// DungeonData keys are:
/// - Entrance
/// - FallbackTile
/// </remarks>
public sealed partial class RoomEntranceDunGen : IDunGenLayer;

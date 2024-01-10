using Content.Server.Shuttles.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Similar to <see cref="GridFillComponent"/> except spawns the grid near to the station.
/// </summary>
[RegisterComponent, Access(typeof(ShuttleSystem))]
public sealed partial class GridSpawnComponent : Component
{
    /// <summary>
    /// Dictionary of groups where each group will have entries selected.
    /// String is just an identifier to make yaml easier.
    /// </summary>
    [DataField(required: true)] public Dictionary<string, GridSpawnGroup> Groups = new();
}

[DataRecord]
public record struct GridSpawnGroup
{
    public List<ResPath> Paths = new();
    public int MinCount = 1;
    public int MaxCount = 1;

    /// <summary>
    /// Hide the IFF of the grid.
    /// </summary>
    public bool Hide = false;

    /// <summary>
    /// Should we set the metadata name of a grid. Useful for admin purposes.
    /// </summary>
    public bool NameGrid = false;

    /// <summary>
    /// Should we add this to the station's grids (if possible / relevant).
    /// </summary>
    public bool StationGrid = true;

    public GridSpawnGroup()
    {
    }
}



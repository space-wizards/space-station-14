using Content.Server.Shuttles.Systems;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
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
    [DataField(required: true)] public Dictionary<string, IGridSpawnGroup> Groups = new();
}

public interface IGridSpawnGroup
{
    /// <summary>
    /// Minimum distance to spawn away from the station.
    /// </summary>
    public float MinimumDistance { get; }

    /// <summary>
    /// Maximum distance to spawn away from the station.
    /// </summary>
    public float MaximumDistance { get;  }

    /// <inheritdoc />
    public ProtoId<DatasetPrototype>? NameDataset { get; }

    /// <inheritdoc />
    int MinCount { get; set; }

    /// <inheritdoc />
    int MaxCount { get; set; }

    /// <summary>
    /// Components to be added to any spawned grids.
    /// </summary>
    public ComponentRegistry AddComponents { get; set; }

    /// <summary>
    /// Hide the IFF label of the grid.
    /// </summary>
    public bool Hide { get; set; }

    /// <summary>
    /// Should we set the metadata name of a grid. Useful for admin purposes.
    /// </summary>
    public bool NameGrid { get; set; }

    /// <summary>
    /// Should we add this to the station's grids (if possible / relevant).
    /// </summary>
    public bool StationGrid { get; set; }
}

[DataRecord]
public sealed class DungeonSpawnGroup : IGridSpawnGroup
{
    /// <summary>
    /// Prototypes we can choose from to spawn.
    /// </summary>
    public List<ProtoId<DungeonConfigPrototype>> Protos = new();

    /// <inheritdoc />
    public float MinimumDistance { get; }

    public float MaximumDistance { get; }

    /// <inheritdoc />
    public ProtoId<DatasetPrototype>? NameDataset { get; }

    /// <inheritdoc />
    public int MinCount { get; set; } = 1;

    /// <inheritdoc />
    public int MaxCount { get; set; } = 1;

    /// <inheritdoc />
    public ComponentRegistry AddComponents { get; set; } = new();

    /// <inheritdoc />
    public bool Hide { get; set; } = false;

    /// <inheritdoc />
    public bool NameGrid { get; set; } = false;

    /// <inheritdoc />
    public bool StationGrid { get; set; } = false;
}

[DataRecord]
public sealed class GridSpawnGroup : IGridSpawnGroup
{
    public List<ResPath> Paths = new();

    /// <inheritdoc />
    public float MinimumDistance { get; }

    /// <inheritdoc />
    public float MaximumDistance { get; }
    public ProtoId<DatasetPrototype>? NameDataset { get; }
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
    public ComponentRegistry AddComponents { get; set; } = new();
    public bool Hide { get; set; } = false;
    public bool NameGrid { get; set; } = true;
    public bool StationGrid { get; set; } = true;
}



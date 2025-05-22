using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Components;

/// <summary>
/// A layer inside of <see cref="NewBiomeComponent"/>
/// </summary>
[DataRecord]
public sealed record NewBiomeMetaLayer
{
    /// <summary>
    /// Chunk dimensions for this meta layer.
    /// </summary>
    public int Size = 16;

    /// <summary>
    /// Meta layers that this one requires to be loaded first.
    /// Will ensure all of the chunks for our corresponding area are loaded.
    /// </summary>
    public List<string>? DependsOn;

    /// <summary>
    /// Can this layer be unloaded if no one is in range.
    /// </summary>
    public bool CanUnload = true;

    /// <summary>
    /// Dungeon config to load inside the specified area.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DungeonConfigPrototype> Dungeon = new();
}

[RegisterComponent]
public sealed partial class NewBiomeComponent : Component
{
    /// <summary>
    /// Is there currently a job that's loading.
    /// </summary>
    public bool Loading = false;

    [DataField]
    public int Seed;

    /// <summary>
    /// Layer key and associated data.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, NewBiomeMetaLayer> Layers = new();

    /// <summary>
    /// Data that is currently loaded.
    /// </summary>
    [DataField]
    public Dictionary<string, Dictionary<Vector2i, DungeonData>> LoadedData = new();

    // Wanted to use a bitmask originally but some stuff like mobs may have massive chunk sizes.
    /// <summary>
    /// Flag modified tiles so we don't try and unload / reload them.
    /// </summary>
    [DataField]
    public HashSet<Vector2i> ModifiedTiles = new();

    /// <summary>
    /// Bounds loaded by players for this tick.
    /// </summary>
    public List<Box2i> LoadedBounds = new();
}

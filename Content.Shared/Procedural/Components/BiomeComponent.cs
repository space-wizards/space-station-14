using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Components;

/// <summary>
/// A layer inside of <see cref="BiomeComponent"/>
/// </summary>
[DataRecord]
public sealed record BiomeMetaLayer
{
    /// <summary>
    /// Chunk dimensions for this meta layer. Will try to infer it from the first layer of the dungeon if null.
    /// </summary>
    [DataField]
    public int? Size;

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
public sealed partial class BiomeComponent : Component
{
    /// <summary>
    /// Can we load / unload chunks.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Areas queued for preloading. Will add these during <see cref="BiomeLoadJob"/> and then flag as modified so they retain.
    /// </summary>
    [DataField]
    public List<Box2i> PreloadAreas = new();

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
    public Dictionary<string, BiomeMetaLayer> Layers = new();

    /// <summary>
    /// Layer removals that are pending.
    /// </summary>
    [DataField]
    public List<string> PendingRemovals = new();

    /// <summary>
    /// Data that is currently loaded.
    /// </summary>
    [DataField]
    public Dictionary<string, Dictionary<Vector2i, DungeonData>> LoadedData = new();

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

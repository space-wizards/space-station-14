namespace Content.Server.Gatherable.Components;

/// <summary>
///     Allows an entity to be interacted with by hand to yield a random number of a specified loot entity.
/// </summary>
[RegisterComponent, Access(typeof(GatherableByHandSystem))]
public sealed class GatherableByHandComponent : Component
{
    /// <summary>
    ///     The ID of the entity to spawn.
    /// </summary>
    [DataField("loot")]
    public string? Loot { get; set; }

    /// <summary>
    ///     The minimum number of entities that can be dropped when gathered.
    /// </summary>
    [DataField("mindropcount")]
    public int MinDropCount { get; set; } = 1;

    /// <summary>
    ///     The maximum number of entities that can be dropped when gathered.
    /// </summary>
    [DataField("maxdropcount")]
    public int MaxDropCount { get; set; } = 1;

    /// <summary>
    ///     The radius of the circle that loot entities can be randomly spawned in when gathered.
    ///     Centered on the entity.
    /// </summary>
    [DataField("dropradius")]
    public float DropRadius { get; set; } = 1.0f;

    /// <summary>
    ///     The amount of time in seconds it takes to complete the gathering action.
    /// </summary>
    [DataField("harvesttime")]
    public float HarvestTime { get; set; } = 1.0f;
}

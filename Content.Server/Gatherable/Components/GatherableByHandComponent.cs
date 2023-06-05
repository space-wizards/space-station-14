using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.ComponentModel.DataAnnotations;

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
    [DataField("loot", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), Required]
    public string Loot = "default";

    /// <summary>
    ///     The minimum number of entities that can be dropped when gathered.
    /// </summary>
    [DataField("minDropCount")]
    public int MinDropCount = 1;

    /// <summary>
    ///     The maximum number of entities that can be dropped when gathered.
    /// </summary>
    [DataField("maxDropCount")]
    public int MaxDropCount = 1;

    /// <summary>
    ///     The radius of the circle that loot entities can be randomly spawned in when gathered.
    ///     Centered on the entity.
    /// </summary>
    [DataField("dropRadius")]
    public float DropRadius = 1.0f;

    /// <summary>
    ///     The amount of time in seconds it takes to complete the gathering action.
    /// </summary>
    [DataField("harvestTime")]
    public float HarvestTime = 1.0f;
}

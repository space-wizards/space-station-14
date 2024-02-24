using Content.Shared.Botany.Systems;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Gives all produce <see cref="ProduceComponent"/> and creates a new plant to use for its seed.
/// Randomly removed by mutations.
/// </summary>
[RegisterComponent, Access(typeof(PlantSeedsSystem))]
public sealed partial class PlantSeedsComponent : Component
{
    /// <summary>
    /// Seed packet prototype to spawn, which then has the plant entity set.
    /// This is accessed from the plant prototype so produce yaml doesnt need to specify both the plant and seed packet ids.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Packet = string.Empty;

    /// <summary>
    /// If true then seeds will not be added.
    /// </summary>
    [DataField]
    public bool Seedless;
}

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
    /// If true then seeds will not be added.
    /// </summary>
    [DataField]
    public bool Seedless;
}

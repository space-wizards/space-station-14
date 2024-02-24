using Content.Shared.Botany.Systems;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Does nothing, WYCI.
/// </summary>
[RegisterComponent, Access(typeof(PlantLightSystem))]
public sealed partial class PlantLightComponent : Component
{
    /// <summary>
    /// Ideal light level to grow.
    /// </summary>
    [DataField]
    public float Ideal = 7f;

    /// <summary>
    /// How much light can deviate from the ideal value.
    /// </summary>
    [DataField]
    public float Tolerance = 3f;
}

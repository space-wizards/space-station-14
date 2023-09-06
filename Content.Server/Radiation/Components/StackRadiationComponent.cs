using Content.Server.Radiation.Systems;

namespace Content.Server.Radiation.Components;

/// <summary>
/// Makes radiation source intensity proportional to stack size.
/// </summary>
[RegisterComponent, Access(typeof(RadiationSystem))]
public sealed partial class StackRadiationComponent : Component
{
    /// <summary>
    /// Intensity of a single item, multiplied by stack size for final intensity.
    /// </summary>
    [DataField("baseIntensity", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float BaseIntensity;
}

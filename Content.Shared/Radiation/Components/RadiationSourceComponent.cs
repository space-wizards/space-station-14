using Content.Shared.Radiation.Systems;

namespace Content.Shared.Radiation.Components;

/// <summary>
/// Irradiate all objects in range.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedRadiationSystem))]
public sealed partial class RadiationSourceComponent : Component
{
    /// <summary>
    /// From there radiation rays will travel over distance and loose intensity
    /// when hit radiation blocker.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 1;

    /// <summary>
    /// Defines how fast radiation rays will loose intensity
    /// over distance. The bigger the value, the shorter range
    /// of radiation source will be.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Slope = 0.5f;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

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
    /// Radiation intensity in center of the source in rads per second.
    /// From there radiation rays will travel over distance and lose intensity
    /// when hit radiation blocker.
    /// </summary>
    [DataField]
    public float Intensity = 1;

    /// <summary>
    /// Defines how fast radiation rays will lose intensity
    /// over distance. The bigger the value, the shorter the range
    /// of radiation source will be.
    /// </summary>
    [DataField]
    public float Slope = 0.5f;

    /// <summary>
    /// Whether the source currently emits radiation.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}

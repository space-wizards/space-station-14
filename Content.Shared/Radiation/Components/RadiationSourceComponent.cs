namespace Content.Shared.Radiation.Components;

/// <summary>
///     Irradiate all objects in range.
/// </summary>
[RegisterComponent]
public sealed class RadiationSourceComponent : Component
{
    /// <summary>
    ///     Radiation intensity in center of the source. From there
    ///     radiation rays will travel over distance and loose intensity
    ///     when hit <see cref="RadiationBlockerComponent"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("radsPerSecond")]
    public float RadsPerSecond = 1;

    /// <summary>
    ///     Defines how fast radiation rays will loose intensity
    ///     over distance. The bigger the value, the shorter range
    ///     of radiation source will be.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("slope")]
    public float Slope = 1f;
}

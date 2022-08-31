/// <summary>
///     Irradiate all objects in range.
/// </summary>
[RegisterComponent]
public sealed class RadiationSourceComponent : Component
{
    /// <summary>
    ///     How many rads per second receive irradiated object.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("radsPerSecond")]
    public float RadsPerSecond = 1;

    /// <summary>
    ///     Radius of radiation source.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public float Range = 5f;
}

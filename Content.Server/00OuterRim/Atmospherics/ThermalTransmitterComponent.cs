namespace Content.Server._00OuterRim.Atmospherics;

/// <summary>
/// This is used for transferring heat between rooms or to space.
/// </summary>
[RegisterComponent]
public sealed class ThermalTransmitterComponent : Component
{
    /// <summary>
    /// Whether or not to play easy and automatically stop voiding heat into space when temp is less than T20C.
    /// </summary>
    [DataField("easyMode")] public bool EasyMode = true;

    /// <summary>
    /// How many joules per second (aka watts) of heat energy the device can void into space via radiation.
    /// </summary>
    [DataField("watts", required: true)] public float Watts;
}

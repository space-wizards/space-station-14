namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class TemperatureProtectionComponent : Component
{
    /// <summary>
    ///     How much to multiply temperature deltas by.
    /// </summary>
    [DataField("coefficient")]
    public float Coefficient = 1.0f;
}

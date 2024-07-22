namespace Content.Shared.Chemistry.Components;

[RegisterComponent]
public sealed partial class RequiresReactionTemperatureComponent : Component
{
    /// <summary>
    ///     The minimum temperature the reaction can occur at.
    /// </summary>
    [DataField("minTemp")]
    public float MinimumTemperature = 0.0f;

    /// <summary>
    ///     The maximum temperature the reaction can occur at.
    /// </summary>
    [DataField("maxTemp")]
    public float MaximumTemperature = float.PositiveInfinity;
}

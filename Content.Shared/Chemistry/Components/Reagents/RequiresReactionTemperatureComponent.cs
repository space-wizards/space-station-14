using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent]
public sealed partial class RequiresReactionTemperatureComponent : Component
{
    /// <summary>
    ///     The minimum temperature the reaction can occur at.
    /// </summary>
    [DataField("minTemp")]
    public float MinimumTemperature;

    /// <summary>
    ///     The maximum temperature the reaction can occur at.
    /// </summary>
    [DataField("maxTemp")]
    public float MaximumTemperature = float.PositiveInfinity;
}

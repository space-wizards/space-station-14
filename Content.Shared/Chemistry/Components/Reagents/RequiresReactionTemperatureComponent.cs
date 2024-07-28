using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RequiresReactionTemperatureComponent : Component
{
    /// <summary>
    ///     The minimum temperature the reaction can occur at.
    /// </summary>
    [DataField("minTemp"), AutoNetworkedField]
    public float MinimumTemperature;

    /// <summary>
    ///     The maximum temperature the reaction can occur at.
    /// </summary>
    [DataField("maxTemp"), AutoNetworkedField]
    public float MaximumTemperature = float.PositiveInfinity;
}

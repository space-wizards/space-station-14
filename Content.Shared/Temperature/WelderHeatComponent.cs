using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature;

/// <summary>
///     This is used for a heating tool to heat up a solution.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WelderHeatComponent : Component
{
    /// <summary>
    ///     How much thermal energy is added per do-after step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatPerUse = 100f;

    /// <summary>
    ///     The maximum temperature the tool can heat a solution to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 400f;

    /// <summary>
    ///     The amount of fuel consumed per do-after step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FuelConsumptionPerHeat = 1f;

    /// <summary>
    ///     How long each heating do-after step takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DoAfterDelay = 1.5f;
}

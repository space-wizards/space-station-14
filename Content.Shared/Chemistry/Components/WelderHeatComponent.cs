using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;


/// This is used for a welder to heat up a solution.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WelderHeatComponent : Component
{
    /// How much thermal energy is added per do-after step.
    [DataField, AutoNetworkedField]
    public float HeatPerUse = 100f;


    /// The maximum temperature the welder can heat a solution to.
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 400f;

    /// The amount of fuel consumed per do-after step.
    [DataField, AutoNetworkedField]
    public float FuelConsumptionPerHeat = 1f;

    /// How long each heating do-after step takes.
    [DataField, AutoNetworkedField]
    public float DoAfterDelay = 1.5f;
}

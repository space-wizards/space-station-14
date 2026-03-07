using Robust.Shared.GameStates;

namespace Content.Shared.Temperature;

/// <summary>
///     This is used for a tool that can be used to heat up something.
///     The tool must handle <see cref="HeaterAttemptEvent"/> and <see cref="HeaterConsumedEvent"/> to define its cost and readiness.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeaterToolComponent : Component
{
    /// <summary>
    ///     How much thermal energy is added per do-after step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatPerUse = 100f;

    /// <summary>
    ///     The maximum temperature the tool can heat a target to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 400f;

    /// <summary>
    ///     How long each heating do-after step takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DoAfterDelay = 1.5f;
}

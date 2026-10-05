using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
///     This is used for a tool that can be used to heat up something.
///     The tool must handle <see cref="HeaterAttemptEvent"/> and <see cref="HeaterConsumedEvent"/> to define its cost and readiness.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedTemperatureSystem))]
public sealed partial class HeaterToolComponent : Component
{
    /// <summary>
    ///     The thermal conductance of the tool.
    ///     This determines how fast heat is transferred to the target based on temperature difference.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Conductivity = 10f;

    /// <summary>
    ///     The maximum temperature the tool can heat a target to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 400f;

    /// <summary>
    ///     How long each heating do-after step takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterDelay = TimeSpan.FromSeconds(1.5);
}

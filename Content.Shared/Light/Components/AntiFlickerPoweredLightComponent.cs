using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// When added to a <see cref="PoweredLightComponent"/>, the light will after changing state have a delay introduced before it can change back.
/// If the light attempts to change state during this delay, it will automatically adjust to it once the delay is over.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AntiFlickerPoweredLightComponent : Component
{
    /// <summary>
    /// The minimum time that a light must be in a state before it can switch over.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RequiredMinimumTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The last time the light was turned on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastTurnOnTime;

    /// <summary>
    /// The last time the light was turned off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastTurnOffTime;

    /// <summary>
    /// If true, the light will be checking if the state should be changed after the delay is over.
    /// </summary>
    /// <remarks>
    /// This is set to true when a light gets switched during a delay e.g. "off -> on -> off" so that the light is not stuck in its "on" state.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool CheckUpdate;
}

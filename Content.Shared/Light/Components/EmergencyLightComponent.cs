using Content.Shared.Light.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

/// <summary>
///     Component that represents an emergency light, it has an internal battery that charges when the power is on.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EmergencyLightSystem))]
public sealed partial class EmergencyLightComponent : Component
{
    /// <summary>
    ///     The current state of the emergency light.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EmergencyLightState State;

    /// <summary>
    ///     Is this emergency light forced on for some reason and cannot be disabled through normal means
    ///     (i.e. blue alert or higher?)
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool ForciblyEnabled = false;

    /// <summary>
    ///     The wattage of the emergency light.
    /// </summary>
    [DataField]
    public float Wattage = 5;

    /// <summary>
    ///     The wattage of the emergency light when charging.
    /// </summary>
    [DataField]
    public float ChargingWattage = 60;

    /// <summary>
    ///     The efficiency of the emergency light when charging.
    /// </summary>
    [DataField]
    public float ChargingEfficiency = 0.85f;

    /// <summary>
    ///     The text to display for each state of the emergency light.
    /// </summary>
    public Dictionary<EmergencyLightState, LocId> BatteryStateText = new()
    {
        { EmergencyLightState.Full, "emergency-light-component-light-state-full" },
        { EmergencyLightState.Empty, "emergency-light-component-light-state-empty" },
        { EmergencyLightState.Charging, "emergency-light-component-light-state-charging" },
        { EmergencyLightState.On, "emergency-light-component-light-state-on" }
    };
}

/// <summary>
///     The state of the emergency light.
/// </summary>
[Serializable, NetSerializable]
public enum EmergencyLightState : byte
{
    Charging,
    Full,
    Empty,
    On
}

/// <summary>
///     Event for when the state of the emergency light changes.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmergencyLightEvent(EmergencyLightState state) : EntityEventArgs
{
    public EmergencyLightState State { get; } = state;
}

/// <summary>
///     The visuals of the emergency light.
/// </summary>
[Serializable, NetSerializable]
public enum EmergencyLightVisuals
{
    On,
    Color
}

/// <summary>
///     The visual layers of the emergency light.
/// </summary>
[Serializable, NetSerializable]
public enum EmergencyLightVisualLayers
{
    Base,
    LightOff,
    LightOn,
}

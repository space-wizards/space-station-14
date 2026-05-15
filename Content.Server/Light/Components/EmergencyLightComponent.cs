using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;

namespace Content.Server.Light.Components;

/// <summary>
///     Component that represents an emergency light, it has an internal battery that charges when the power is on.
/// </summary>
[RegisterComponent, Access(typeof(EmergencyLightSystem))]
public sealed partial class EmergencyLightComponent : SharedEmergencyLightComponent
{
    [ViewVariables]
    public EmergencyLightState State;

    /// <summary>
    ///     Is this emergency light forced on for some reason and cannot be disabled through normal means
    ///     (i.e. blue alert or higher?)
    /// </summary>
    public bool ForciblyEnabled = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("wattage")]
    public float Wattage = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("chargingWattage")]
    public float ChargingWattage = 60;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("chargingEfficiency")]
    public float ChargingEfficiency = 0.85f;

    public Dictionary<EmergencyLightState, string> BatteryStateText = new()
    {
        { EmergencyLightState.Full, "emergency-light-component-light-state-full" },
        { EmergencyLightState.Empty, "emergency-light-component-light-state-empty" },
        { EmergencyLightState.Charging, "emergency-light-component-light-state-charging" },
        { EmergencyLightState.On, "emergency-light-component-light-state-on" }
    };
}

public enum EmergencyLightState : byte
{
    Charging,
    Full,
    Empty,
    On
}

public sealed class EmergencyLightEvent : EntityEventArgs
{
    public EmergencyLightState State { get; }

    public EmergencyLightEvent(EmergencyLightState state)
    {
        State = state;
    }
}
